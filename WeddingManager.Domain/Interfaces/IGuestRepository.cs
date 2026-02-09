using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IGuestRepository
{
    Task<Guest?> GetByIdAsync(Guid id);
    Task<IEnumerable<Guest>> GetByWeddingIdAsync(Guid weddingId);
    Task<IEnumerable<Guest>> GetByIdsAsync(Guid weddingId, IEnumerable<Guid> guestIds);
    Task<Guest?> GetByEmailAsync(Guid weddingId, string email);
    Task<int> CountByWeddingIdAsync(Guid weddingId);
    Task AddAsync(Guest guest);
    Task AddRangeAsync(IEnumerable<Guest> guests);
    Task<IEnumerable<string>> GetEmailsByWeddingIdAsync(Guid weddingId);
    Task<IEnumerable<string>> GetExistingEmailsAsync(Guid weddingId, IEnumerable<string> emails);
    Task UpdateAsync(Guest guest);
    Task DeleteAsync(Guid id);
}
