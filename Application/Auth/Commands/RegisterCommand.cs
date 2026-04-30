using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Entities.Enums;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Domain.Exceptions;
using MediatR;

namespace AUTH_Sevice.Application.Auth.Commands
{

    public record RegisterCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName) : IRequest<UserDto>;

    public class RegisterCommandHandler(
        IUserRepository userRepo,
        IUnitOfWork uow,
        IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, UserDto>
    {
        public async Task<UserDto> Handle(RegisterCommand request, CancellationToken ct)
        {
            var existing = await userRepo.GetByEmailAsync(request.Email, ct);
            if (existing is not null)
                throw new EmailAlreadyExistsException(request.Email);

            var passwordHash = passwordHasher.Hash(request.Password);
            var user = User.Create(request.Email, passwordHash, request.FirstName, request.LastName, UserRole.User);

            await userRepo.AddAsync(user, ct);
            await uow.SaveChangesAsync(ct);

            return new UserDto(user.Id, user.Email, user.FirstName, user.LastName,
                user.Role.ToString(), user.IsActive, user.CreatedAt);
        }
    }
}
