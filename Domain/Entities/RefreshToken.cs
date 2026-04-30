namespace AUTH_Sevice.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string CreatedByIp { get; private set; } = string.Empty;
        public DateTime? RevokedAt { get; private set; }
        public string? RevokedReason { get; private set; }
        public Guid UserId { get; private set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsActive => !IsRevoked && !IsExpired;

        private RefreshToken() { } // EF Core

        public static RefreshToken Create(Guid userId, string token, string createdByIp, int expiryDays = 7)
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                CreatedByIp = createdByIp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            };
        }

        public void Revoke(string reason)
        {
            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
        }
    }
}