using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Domain.Exceptions;
using AUTH_Sevice.Infrastructure.Repositories;
using AUTH_Sevice.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace AUTH_Sevice.Application.Auth.Commands
{
    public record RefreshTokenCommand(string RefreshToken, string IpAddress) : IRequest<AuthResponse>;

    public class RefreshTokenCommandHandler(
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUnitOfWork uow,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings) : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
        {
            var existingToken = await refreshTokenRepo.GetByTokenAsync(request.RefreshToken, ct);

            if (existingToken is null || !existingToken.IsActive)
                throw new InvalidRefreshTokenException();

            var user = await userRepo.GetByIdAsync(existingToken.UserId, ct)
                       ?? throw new InvalidRefreshTokenException();

            if (!user.IsActive)
                throw new UserInactiveException();

            // Rotate refresh token
            existingToken.Revoke("rotated");
            refreshTokenRepo.Update(existingToken);

            var newRawToken = tokenService.GenerateRefreshToken();
            var newRefreshToken = RefreshToken.Create(user.Id, newRawToken, request.IpAddress,
                jwtSettings.Value.RefreshTokenExpiryDays);

            await refreshTokenRepo.AddAsync(newRefreshToken, ct);

            var accessToken = tokenService.GenerateAccessToken(user);
            await uow.SaveChangesAsync(ct);

            return new AuthResponse(
                accessToken,
                newRawToken,
                DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes));
        }
    }
}
