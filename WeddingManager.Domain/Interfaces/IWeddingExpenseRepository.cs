using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingExpenseRepository
{
    Task<WeddingExpense?> GetByIdAsync(Guid id);
    Task<IEnumerable<WeddingExpense>> GetByWeddingIdAsync(Guid weddingId);
    Task<IEnumerable<WeddingExpense>> GetByWeddingIdAndCategoryAsync(Guid weddingId, ExpenseCategory category);
    Task<Dictionary<ExpenseCategory, decimal>> GetCategoryTotalsAsync(Guid weddingId);
    Task<decimal> GetTotalAmountAsync(Guid weddingId);
    Task AddAsync(WeddingExpense expense);
    Task UpdateAsync(WeddingExpense expense);
    Task DeleteAsync(Guid id);
}
