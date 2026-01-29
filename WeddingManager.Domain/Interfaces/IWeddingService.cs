using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingService
{
    Task<Result<IEnumerable<Wedding>>> GetAllAsync();
    Task<Result<Wedding>> GetByIdAsync(Guid id);
    Task<Result<Wedding>> GetByIdOrSlugAsync(string idOrSlug);
    Task<Result> AddAsync(Wedding wedding);
    Task<Result> UpdateAsync(Wedding wedding);
    Task<Result> DeleteAsync(Guid id);
}
