
using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Domain.Entities.Enums;
using AUTH_Sevice.Infrastructure.Repositories;
using AutoMapper;

namespace AUTH_Sevice.Infrastructure.Services
{
    public class AdminUserService
    {
        private readonly IUserRepository _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminUserService> _logger;

        public AdminUserService(
            IUserRepository repo,
            IMapper mapper,
            ILogger<AdminUserService> logger)
        {
            _repo = repo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<UserListDto>> GetAllUsersAsync(CancellationToken ct = default)
        {
            var users = await _repo.GetAllAsync(ct);
            return _mapper.Map<IEnumerable<UserListDto>>(users);
        }

        public async Task<UserListDto> GetUserByIdAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"User {id} not found.");
            return _mapper.Map<UserListDto>(user);
        }

        public async Task<UserListDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default)
        {
            // Vérification unicité email
            var existing = await _repo.GetByEmailAsync(dto.Email, ct);
            if (existing is not null)
                throw new InvalidOperationException($"Email {dto.Email} already in use.");

            User user = new ()
            {
                Id = Guid.NewGuid(),
                FirstName = dto.Name,
                LastName = dto.Name,
                Email = dto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.User,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(user, ct);
            return _mapper.Map<UserListDto>(created);
        }

        public async Task<UserListDto> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"User {id} not found.");

            user.FirstName = dto.Name;
            user.Email = dto.Email.ToLower();
            user.Role = UserRole.User;
            user.IsActive = dto.isActive;

            var updated = await _repo.UpdateAsync(user, ct);
            return _mapper.Map<UserListDto>(updated);
        }

        public async Task DeleteUserAsync(Guid id, CancellationToken ct = default)
            => await _repo.DeleteAsync(id, ct);

        public async Task<UserListDto> UpdateStatusAsync(Guid id, UpdateUserStatusDto dto, CancellationToken ct = default)
        {
            //var validStatuses = new[] { UserStatus., UserStatus.Inactive, UserStatus.Blocked };
            //if (!validStatuses.Contains(dto.isActive))
            //    throw new ArgumentException($"Invalid status: {dto.isActive}");

            var user = await _repo.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"User {id} not found.");

            user.IsActive = dto.isActive;
            var updated = await _repo.UpdateAsync(user, ct);

            _logger.LogWarning("User {Id} status changed to {Status}", id, dto.isActive);
            return _mapper.Map<UserListDto>(updated);
        }
    }
}
