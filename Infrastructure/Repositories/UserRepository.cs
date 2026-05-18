using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AUTH_Sevice.Infrastructure.Repositories
{
    public class UserRepositoryADMIN : IUserRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UserRepository> _logger;

        public UserRepositoryADMIN(AppDbContext db, ILogger<UserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
            => await _db.Users.AsNoTracking().ToListAsync(ct);

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
            => await _db.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

        public async Task<User> CreateAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("User created: {Email}", user.Email);
            return user;
        }

        public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);
            return user;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _db.Users.FindAsync([id], ct)
                ?? throw new KeyNotFoundException($"User {id} not found.");
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("User deleted: {Id}", id);
        }
    }
}
