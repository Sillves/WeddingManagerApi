using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IGuestRepository
{
    Task<Guest?> GetByIdAsync(Guid id);
    Task<IEnumerable<Guest>> GetByWeddingIdAsync(Guid weddingId);
    Task<IEnumerable<Guest>> GetByIdsAsync(Guid weddingId, IEnumerable<Guid> guestIds);
    Task<Guest?> GetByEmailAsync(Guid weddingId, string email);
    Task AddAsync(Guest guest);
    Task UpdateAsync(Guest guest);
    Task DeleteAsync(Guid id);
}
