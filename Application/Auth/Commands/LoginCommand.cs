using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Domain.Exceptions;
using AUTH_Sevice.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Options;


namespace AUTH_Sevice.Application.Auth.Commands
{
    public record LoginCommand(string Email, string Password, string IpAddress) : IRequest<AuthResponse>;
 
// Handler
public class LoginCommandHandler(
    IUserRepository userRepo,
    IRefreshTokenRepository refreshTokenRepo,
    IAuditLogRepository auditRepo,
    IUnitOfWork uow,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IOptions<JwtSettings> jwtSettings) : IRequestHandler<LoginCommand, AuthResponse>
    {
        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
        {
            var user = await userRepo.GetByEmailAsync(request.Email, ct);

            if (user is null || !user.IsActive)
            {
                await LogAudit(null, "LOGIN_FAILED", request.IpAddress, false, "User not found", ct);
                throw new InvalidCredentialsException();
            }

            if (user.IsLockedOut())
            {
                await LogAudit(user.Id, "LOGIN_LOCKED", request.IpAddress, false, "Account locked", ct);
                throw new UserLockedException(user.LockedUntil!.Value);
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                user.RecordFailedLogin();
                userRepo.Update(user);
                await LogAudit(user.Id, "LOGIN_FAILED", request.IpAddress, false, "Bad password", ct);
                await uow.SaveChangesAsync(ct);
                throw new InvalidCredentialsException();
            }

            // Success
            user.RecordSuccessfulLogin();

            var accessToken = tokenService.GenerateAccessToken(user);
            var rawRefreshToken = tokenService.GenerateRefreshToken();
            var refreshToken = RefreshToken.Create(user.Id, rawRefreshToken, request.IpAddress,
                jwtSettings.Value.RefreshTokenExpiryDays);

            user.AddRefreshToken(refreshToken);
            userRepo.Update(user);
            await refreshTokenRepo.AddAsync(refreshToken, ct);
            await LogAudit(user.Id, "LOGIN_SUCCESS", request.IpAddress, true, null, ct);
            await uow.SaveChangesAsync(ct);

            return new AuthResponse(
                accessToken,
                rawRefreshToken,
                DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes));
        }

        private async Task LogAudit(Guid? userId, string action, string ip, bool success, string? details, CancellationToken ct)
        {
            var log = AuditLog.Create(userId, action, ip, success, details);
            await auditRepo.AddAsync(log, ct);
        }
    }
}
