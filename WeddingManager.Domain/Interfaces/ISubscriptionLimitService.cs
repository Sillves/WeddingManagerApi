namespace WeddingManager.Domain.Interfaces;

public interface ISubscriptionLimitService
{
    Task EnsureGuestLimitAsync(Guid weddingId);
    Task EnsureEventLimitAsync(Guid weddingId);
    Task EnsureEmailLimitAsync(Guid weddingId, int emailCount);
    Task RecordEmailsSentAsync(Guid userId, int emailCount);
}
