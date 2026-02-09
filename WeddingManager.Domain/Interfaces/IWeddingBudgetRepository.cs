using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingBudgetRepository
{
    Task<WeddingBudget?> GetByWeddingIdAsync(Guid weddingId);
    Task UpsertAsync(WeddingBudget budget);
}
