using AUTH_Sevice.Domain.Entities;

namespace AUTH_Sevice.Application.Common.Intefaces
{

    //public interface IUserRepository
    //{
    //    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    //    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    //    Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default);
    //    Task AddAsync(User user, CancellationToken ct = default);
    //    void Update(User user);
    //}

    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
        Task AddAsync(RefreshToken token, CancellationToken ct = default);
        void Update(RefreshToken token);
    }

    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog log, CancellationToken ct = default);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Guid? GetUserIdFromExpiredToken(string token);
    }

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? Email { get; }
        string? Role { get; }
        string? IpAddress { get; }
    }
}
