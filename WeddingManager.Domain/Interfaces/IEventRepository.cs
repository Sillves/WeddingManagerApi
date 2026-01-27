using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Interfaces;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event?> GetByNameAsync(string name);
    Task AddAsync(Event @event);
    Task UpdateAsync(Event @event);
    Task DeleteAsync(Guid id);
    Task<EventGuestChangeResult> AddGuestToEventAsync(Guid eventId, Guid guestId);
    Task<EventGuestChangeResult> RemoveGuestFromEventAsync(Guid eventId, Guid guestId);
    Task<IEnumerable<Event>> GetByWeddingIdAsync(Guid weddingId);
    
}
