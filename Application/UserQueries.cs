using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Exceptions;
using AUTH_Sevice.Infrastructure.Repositories;
using MediatR;
namespace AUTH_Sevice.Application
{
    public record GetCurrentUserQuery(Guid UserId) : IRequest<UserProfileDto>;

    public class GetCurrentUserQueryHandler(IUserRepository userRepo)
        : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
    {
        public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken ct)
        {
            var user = await userRepo.GetByIdAsync(request.UserId, ct)
                       ?? throw new UserNotFoundException(request.UserId.ToString());

            return new UserProfileDto(user.Id, user.Email, user.FirstName,
                user.LastName, user.Role.ToString(), user.CreatedAt);
        }
    }

    // GetAll (Admin)
    public record GetAllUsersQuery : IRequest<IEnumerable<UserDto>>;

    public class GetAllUsersQueryHandler(IUserRepository userRepo)
        : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
    {
        public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
        {
            var users = await userRepo.GetAllAsync(ct);
            return users.Select(u => new UserDto(
                u.Id, u.Email, u.FirstName, u.LastName,
                u.Role.ToString(), u.IsActive, u.CreatedAt));
        }
    }
}
