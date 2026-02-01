using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingRepository
{
    Task<IEnumerable<Wedding>> GetAllAsync(Guid userId);
    Task<Wedding?> GetByIdAsync(Guid id);
    Task<Wedding?> GetByIdOrSlugAsync(string idOrSlug);
    Task<IEnumerable<Wedding>> GetWeddingsWithMediaOlderThanAsync(DateTime cutoffDate);
    Task AddAsync(Wedding wedding);
    Task UpdateAsync(Wedding wedding);
    Task DeleteAsync(Guid id);
}
