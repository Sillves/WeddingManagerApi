using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingExpenseService
{
    Task<Result<WeddingExpenseDto>> GetByIdAsync(Guid expenseId);
    Task<Result<IEnumerable<WeddingExpenseDto>>> GetByWeddingIdAsync(Guid weddingId);
    Task<Result<IEnumerable<WeddingExpenseDto>>> GetByWeddingIdAndCategoryAsync(Guid weddingId, ExpenseCategory category);
    Task<Result<WeddingExpenseSummaryDto>> GetSummaryAsync(Guid weddingId);
    Task<Result<WeddingExpenseDto>> CreateExpenseAsync(Guid weddingId, CreateWeddingExpenseRequestDto requestDto);
    Task<Result<WeddingExpenseDto>> UpdateExpenseAsync(Guid expenseId, UpdateWeddingExpenseRequestDto requestDto);
    Task<Result> DeleteExpenseAsync(Guid expenseId);
}
