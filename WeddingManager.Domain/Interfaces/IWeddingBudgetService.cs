using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingBudgetService
{
    Task<Result<WeddingBudgetDto?>> GetByWeddingIdAsync(Guid weddingId);
    Task<Result<WeddingBudgetDto>> UpsertBudgetAsync(Guid weddingId, UpsertWeddingBudgetRequestDto request);
}
