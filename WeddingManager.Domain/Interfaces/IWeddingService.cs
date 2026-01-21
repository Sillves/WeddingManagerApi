using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingService
{
    Task<IEnumerable<Wedding>> GetAllAsync();
    Task<Wedding?> GetByIdAsync(Guid id);
    Task AddAsync(Wedding wedding);
    Task UpdateAsync(Wedding wedding);
    Task DeleteAsync(Guid id);
}