using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface ISubscriptionUsageRepository
{
    Task<SubscriptionUsage?> GetByPeriodAsync(Guid userId, int year, int month);
    Task AddAsync(SubscriptionUsage usage);
    Task UpdateAsync(SubscriptionUsage usage);
}
