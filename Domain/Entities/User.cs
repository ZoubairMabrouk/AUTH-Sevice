using AUTH_Sevice.Domain.Entities.Enums;
using AUTH_Sevice.Domain.Events;
namespace AUTH_Sevice.Domain.Entities
{
    public class User
    {
        private readonly List<RefreshToken> _refreshTokens = new();

        public Guid Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public int FailedLoginAttempts { get; private set; }
        public DateTime? LockedUntil { get; private set; }

        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        private User() { } 

        public static User Create(string email, string passwordHash, string firstName, string lastName, UserRole role = UserRole.User)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email.ToLowerInvariant(),
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                FailedLoginAttempts = 0
            };

            user._domainEvents.Add(new UserRegisteredEvent(user.Id, user.Email));
            return user;
        }

        public void RecordFailedLogin()
        {
            FailedLoginAttempts++;
            if (FailedLoginAttempts >= 5)
            {
                LockedUntil = DateTime.UtcNow.AddMinutes(15);
                _domainEvents.Add(new UserLockedEvent(Id, LockedUntil.Value));
            }
            UpdatedAt = DateTime.UtcNow;
        }

        public void RecordSuccessfulLogin()
        {
            FailedLoginAttempts = 0;
            LockedUntil = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsLockedOut() =>
            LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

        public void AddRefreshToken(RefreshToken token)
        {
            foreach (var old in _refreshTokens.Where(t => t.IsActive))
                old.Revoke("replaced by new token");

            _refreshTokens.Add(token);
            UpdatedAt = DateTime.UtcNow;
        }

        public RefreshToken? GetActiveRefreshToken(string token) =>
            _refreshTokens.SingleOrDefault(t => t.Token == token && t.IsActive);

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
