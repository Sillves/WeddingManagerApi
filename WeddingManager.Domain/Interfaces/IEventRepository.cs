using WeddingManager.Domain.DTO;
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
    Task<EventGuestBatchChangeResultDto> AddGuestsToEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds);
    Task<EventGuestChangeResult> RemoveGuestFromEventAsync(Guid eventId, Guid guestId);
    Task<EventGuestBatchRemoveResultDto> RemoveGuestsFromEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds);
    Task<IEnumerable<Event>> GetByWeddingIdAsync(Guid weddingId);
    
}
