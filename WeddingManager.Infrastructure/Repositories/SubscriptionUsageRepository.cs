using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class SubscriptionUsageRepository(WeddingDbContext context) : ISubscriptionUsageRepository
{
    public async Task<SubscriptionUsage?> GetByPeriodAsync(Guid userId, int year, int month)
    {
        return await context.SubscriptionUsages
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Year == year && u.Month == month);
    }

    public async Task AddAsync(SubscriptionUsage usage)
    {
        await context.SubscriptionUsages.AddAsync(usage);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SubscriptionUsage usage)
    {
        context.SubscriptionUsages.Update(usage);
        await context.SaveChangesAsync();
    }
}
