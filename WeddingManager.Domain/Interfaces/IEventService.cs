using WeddingManager.Domain.Enums;
using WeddingManager.Domain.DTO;

namespace WeddingManager.Domain.Interfaces;

public interface IEventService
{
    Task<EventDto> CreateEventAsync(Guid weddingId, CreateEventRequestDto requestDto);
    Task<EventDto?> GetByIdAsync(Guid eventId);
    Task<IEnumerable<EventDto>> GetAllAsync();
    Task<IEnumerable<EventDto>> GetByWeddingIdAsync(Guid weddingId);
    Task<EventDto?> GetByNameAsync(string name);
    Task<EventDto> UpdateEventAsync(Guid eventId, UpdateEventRequestDto requestDto);
    Task DeleteEventAsync(Guid eventId);
    Task<EventGuestChangeResult> AddGuestToEventAsync(Guid eventId, Guid guestId);
    Task<EventGuestChangeResult> RemoveGuestFromEventAsync(Guid eventId, Guid guestId);
}
