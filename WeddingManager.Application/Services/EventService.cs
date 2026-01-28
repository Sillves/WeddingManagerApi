using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Services;

public class EventService(
    IEventRepository eventRepository,
    ISubscriptionLimitService subscriptionLimitService,
    IMapper mapper) : IEventService
{
    public async Task<EventDto> CreateEventAsync(Guid weddingId, CreateEventRequestDto requestDto)
    {
        EventValidation.ValidateInput(requestDto);

        await subscriptionLimitService.EnsureEventLimitAsync(weddingId);

        var @event = mapper.Map<Event>(requestDto);
        @event.Id = Guid.NewGuid();
        @event.WeddingId = weddingId;

        await eventRepository.AddAsync(@event);
        return mapper.Map<EventDto>(@event);
    }

    public async Task<EventDto?> GetByIdAsync(Guid eventId)
    {
        var @event = await eventRepository.GetByIdAsync(eventId);
        return @event == null ? null : mapper.Map<EventDto>(@event);
    }

    public async Task<IEnumerable<EventDto>> GetAllAsync()
    {
        var events = await eventRepository.GetAllAsync();
        return mapper.Map<IEnumerable<EventDto>>(events);
    }

    public async Task<IEnumerable<EventDto>> GetByWeddingIdAsync(Guid weddingId)
    {
        var events = await eventRepository.GetByWeddingIdAsync(weddingId);
        return mapper.Map<IEnumerable<EventDto>>(events);
    }

    public async Task<EventDto?> GetByNameAsync(string name)
    {
        var @event = await eventRepository.GetByNameAsync(name);
        return @event == null ? null : mapper.Map<EventDto>(@event);
    }

    public async Task<EventDto> UpdateEventAsync(Guid eventId, UpdateEventRequestDto requestDto)
    {
        EventValidation.ValidateInput(requestDto);

        var @event = await eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        mapper.Map(requestDto, @event);
        await eventRepository.UpdateAsync(@event);
        return mapper.Map<EventDto>(@event);
    }

    public async Task DeleteEventAsync(Guid eventId)
    {
        var existing = await eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        await eventRepository.DeleteAsync(existing.Id);
    }

    public Task<EventGuestChangeResult> AddGuestToEventAsync(Guid eventId, Guid guestId) =>
        eventRepository.AddGuestToEventAsync(eventId, guestId);

    public Task<EventGuestBatchChangeResultDto> AddGuestsToEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds) =>
        eventRepository.AddGuestsToEventAsync(eventId, guestIds);

    public Task<EventGuestChangeResult> RemoveGuestFromEventAsync(Guid eventId, Guid guestId) =>
        eventRepository.RemoveGuestFromEventAsync(eventId, guestId);

    public Task<EventGuestBatchRemoveResultDto> RemoveGuestsFromEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds) =>
        eventRepository.RemoveGuestsFromEventAsync(eventId, guestIds);
}
