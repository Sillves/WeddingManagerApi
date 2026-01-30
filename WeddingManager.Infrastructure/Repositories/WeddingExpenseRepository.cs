using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class WeddingExpenseRepository(WeddingDbContext context) : IWeddingExpenseRepository
{
    public async Task<WeddingExpense?> GetByIdAsync(Guid id)
    {
        return await context.WeddingExpenses
            .Include(e => e.Wedding)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<WeddingExpense>> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.WeddingExpenses
            .Where(e => e.WeddingId == weddingId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<WeddingExpense>> GetByWeddingIdAndCategoryAsync(Guid weddingId, ExpenseCategory category)
    {
        return await context.WeddingExpenses
            .Where(e => e.WeddingId == weddingId && e.Category == category)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<Dictionary<ExpenseCategory, decimal>> GetCategoryTotalsAsync(Guid weddingId)
    {
        return await context.WeddingExpenses
            .Where(e => e.WeddingId == weddingId)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.Category, x => x.Total);
    }

    public async Task<decimal> GetTotalAmountAsync(Guid weddingId)
    {
        return await context.WeddingExpenses
            .Where(e => e.WeddingId == weddingId)
            .SumAsync(e => e.Amount);
    }

    public async Task AddAsync(WeddingExpense expense)
    {
        await context.WeddingExpenses.AddAsync(expense);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WeddingExpense expense)
    {
        context.WeddingExpenses.Update(expense);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var expense = await context.WeddingExpenses.FindAsync(id);
        if (expense != null)
        {
            context.WeddingExpenses.Remove(expense);
            await context.SaveChangesAsync();
        }
    }
}
