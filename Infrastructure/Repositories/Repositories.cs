using AUTH_Sevice.Application.Common.Intefaces;
using AUTH_Sevice.Domain.Entities;
using AUTH_Sevice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AUTH_Sevice.Infrastructure.Repositories
{
    public class UserRepository(AppDbContext context) : IUserRepository
    {
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            await context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default) =>
            await context.Users.AsNoTracking().ToListAsync(ct);

        public async Task AddAsync(User user, CancellationToken ct = default) =>
            await context.Users.AddAsync(user, ct);

        public void Update(User user) =>
            context.Users.Update(user);
    }

    public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default) =>
            await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, ct);

        public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
            await context.RefreshTokens.AddAsync(token, ct);

        public void Update(RefreshToken token) =>
            context.RefreshTokens.Update(token);
    }

    public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
    {
        public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
            await context.AuditLogs.AddAsync(log, ct);
    }

    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            await context.SaveChangesAsync(ct);
    }

}
