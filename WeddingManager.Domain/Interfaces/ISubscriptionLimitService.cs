using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface ISubscriptionLimitService
{
    Task<Result> EnsureGuestLimitAsync(Guid weddingId);
    Task<Result> EnsureEventLimitAsync(Guid weddingId);
    Task<Result> EnsureEmailLimitAsync(Guid weddingId, int emailCount);
    Task<Result> RecordEmailsSentAsync(Guid userId, int emailCount);
}
