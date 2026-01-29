using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IEventService
{
    Task<Result<EventDto>> CreateEventAsync(Guid weddingId, CreateEventRequestDto requestDto);
    Task<Result<EventDto>> GetByIdAsync(Guid eventId);
    Task<Result<IEnumerable<EventDto>>> GetAllAsync();
    Task<Result<IEnumerable<EventDto>>> GetByWeddingIdAsync(Guid weddingId);
    Task<Result<EventDto>> GetByNameAsync(string name);
    Task<Result<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequestDto requestDto);
    Task<Result> DeleteEventAsync(Guid eventId);
    Task<Result<EventGuestChangeResult>> AddGuestToEventAsync(Guid eventId, Guid guestId);
    Task<Result<EventGuestBatchChangeResultDto>> AddGuestsToEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds);
    Task<Result<EventGuestChangeResult>> RemoveGuestFromEventAsync(Guid eventId, Guid guestId);
    Task<Result<EventGuestBatchRemoveResultDto>> RemoveGuestsFromEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds);
}
