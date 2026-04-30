namespace AUTH_Sevice.Domain.Events
{
    public abstract class DomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }
    public class UserRegisteredEvent(Guid userId, string email) : DomainEvent
    {
        public Guid UserId { get; } = userId;
        public string Email { get; } = email;
    }

    public class UserLockedEvent(Guid userId, DateTime lockedUntil) : DomainEvent
    {
        public Guid UserId { get; } = userId;
        public DateTime LockedUntil { get; } = lockedUntil;
    }
}
