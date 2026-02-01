using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IMediaRepository
{
    Task<Media?> GetByIdAsync(Guid id);
    Task<IEnumerable<Media>> GetByWeddingIdAsync(Guid weddingId);
    Task AddAsync(Media media);
    Task DeleteAsync(Guid id);
}
