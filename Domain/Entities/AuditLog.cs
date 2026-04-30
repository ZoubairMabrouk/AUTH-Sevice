namespace AUTH_Sevice.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; private set; }
        public Guid? UserId { get; private set; }
        public string Action { get; private set; } = string.Empty;
        public string IpAddress { get; private set; } = string.Empty;
        public bool Success { get; private set; }
        public string? Details { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private AuditLog() { }

        public static AuditLog Create(Guid? userId, string action, string ipAddress, bool success, string? details = null) =>
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                IpAddress = ipAddress,
                Success = success,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };
    }
}
