using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingWebsiteRepository
{
    Task<WeddingWebsite?> GetByIdAsync(Guid id);
    Task<WeddingWebsite?> GetByWeddingIdAsync(Guid weddingId);
    Task<WeddingWebsite?> GetPublishedBySlugAsync(string slug);
    Task AddAsync(WeddingWebsite website);
    Task UpdateAsync(WeddingWebsite website);
    Task DeleteAsync(Guid id);
}
