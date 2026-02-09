using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class WeddingBudgetRepository(WeddingDbContext context) : IWeddingBudgetRepository
{
    public async Task<WeddingBudget?> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.WeddingBudgets
            .Include(b => b.Allocations)
            .FirstOrDefaultAsync(b => b.WeddingId == weddingId);
    }

    public async Task UpsertAsync(WeddingBudget budget)
    {
        var existing = await context.WeddingBudgets
            .Include(b => b.Allocations)
            .FirstOrDefaultAsync(b => b.WeddingId == budget.WeddingId);

        if (existing == null)
        {
            await context.WeddingBudgets.AddAsync(budget);
        }
        else
        {
            existing.TotalBudget = budget.TotalBudget;
            existing.UpdatedAt = budget.UpdatedAt;

            context.BudgetAllocations.RemoveRange(existing.Allocations);

            foreach (var allocation in budget.Allocations)
            {
                allocation.WeddingBudgetId = existing.Id;
                await context.BudgetAllocations.AddAsync(allocation);
            }
        }

        await context.SaveChangesAsync();
    }
}
