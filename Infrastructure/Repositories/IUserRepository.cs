using AUTH_Sevice.Domain.Entities;

namespace AUTH_Sevice.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<User> CreateAsync(User user, CancellationToken ct = default);
        Task<User> UpdateAsync(User user, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
