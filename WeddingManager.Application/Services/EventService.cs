using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class EventService(
    IEventRepository eventRepository,
    ISubscriptionLimitService subscriptionLimitService,
    ApplicationMapper mapper) : IEventService
{
    public async Task<Result<EventDto>> CreateEventAsync(Guid weddingId, CreateEventRequestDto requestDto)
    {
        var validationResult = EventValidation.ValidateInput(requestDto);
        if (!validationResult.IsSuccess)
        {
            return Result<EventDto>.Fail(validationResult.Errors);
        }

        var limitResult = await subscriptionLimitService.EnsureEventLimitAsync(weddingId);
        if (!limitResult.IsSuccess)
        {
            return Result<EventDto>.Fail(limitResult.Errors);
        }

        var @event = mapper.CreateEventRequestToEvent(requestDto);
        @event.Id = Guid.NewGuid();
        @event.WeddingId = weddingId;

        await eventRepository.AddAsync(@event);
        return Result<EventDto>.Ok(mapper.EventToDto(@event));
    }

    public async Task<Result<EventDto>> GetByIdAsync(Guid eventId)
    {
        var @event = await eventRepository.GetByIdAsync(eventId);
        return @event == null
            ? Result<EventDto>.Fail(new Error(ErrorCodes.NotFound, $"Event with id {eventId} not found"))
            : Result<EventDto>.Ok(mapper.EventToDto(@event));
    }

    public async Task<Result<IEnumerable<EventDto>>> GetAllAsync()
    {
        var events = await eventRepository.GetAllAsync();
        return Result<IEnumerable<EventDto>>.Ok(mapper.EventsToDto(events));
    }

    public async Task<Result<IEnumerable<EventDto>>> GetByWeddingIdAsync(Guid weddingId)
    {
        var events = await eventRepository.GetByWeddingIdAsync(weddingId);
        return Result<IEnumerable<EventDto>>.Ok(mapper.EventsToDto(events));
    }

    public async Task<Result<EventDto>> GetByNameAsync(string name)
    {
        var @event = await eventRepository.GetByNameAsync(name);
        return @event == null
            ? Result<EventDto>.Fail(new Error(ErrorCodes.NotFound, $"Event with name {name} not found"))
            : Result<EventDto>.Ok(mapper.EventToDto(@event));
    }

    public async Task<Result<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequestDto requestDto)
    {
        var validationResult = EventValidation.ValidateInput(requestDto);
        if (!validationResult.IsSuccess)
        {
            return Result<EventDto>.Fail(validationResult.Errors);
        }

        var @event = await eventRepository.GetByIdAsync(eventId);
        if (@event == null)
        {
            return Result<EventDto>.Fail(new Error(ErrorCodes.NotFound, $"Event with id {eventId} not found"));
        }

        mapper.UpdateEventFromDto(requestDto, @event);
        await eventRepository.UpdateAsync(@event);
        return Result<EventDto>.Ok(mapper.EventToDto(@event));
    }

    public async Task<Result> DeleteEventAsync(Guid eventId)
    {
        var existing = await eventRepository.GetByIdAsync(eventId);
        if (existing == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, $"Event with id {eventId} not found"));
        }

        await eventRepository.DeleteAsync(existing.Id);
        return Result.Ok();
    }

    public async Task<Result<EventGuestChangeResult>> AddGuestToEventAsync(Guid eventId, Guid guestId)
    {
        var result = await eventRepository.AddGuestToEventAsync(eventId, guestId);
        return MapGuestChangeResult(result, "add");
    }

    public async Task<Result<EventGuestBatchChangeResultDto>> AddGuestsToEventAsync(
        Guid eventId,
        IReadOnlyCollection<Guid> guestIds)
    {
        var result = await eventRepository.AddGuestsToEventAsync(eventId, guestIds);
        return result.Status switch
        {
            EventGuestChangeResult.NotFound => Result<EventGuestBatchChangeResultDto>.Fail(
                new Error(ErrorCodes.NotFound, "Event or guest not found")),
            EventGuestChangeResult.Unauthorized => Result<EventGuestBatchChangeResultDto>.Fail(
                new Error(ErrorCodes.Forbidden, "Not allowed to modify this event")),
            _ => Result<EventGuestBatchChangeResultDto>.Ok(result)
        };
    }

    public async Task<Result<EventGuestChangeResult>> RemoveGuestFromEventAsync(Guid eventId, Guid guestId)
    {
        var result = await eventRepository.RemoveGuestFromEventAsync(eventId, guestId);
        return MapGuestChangeResult(result, "remove");
    }

    public async Task<Result<EventGuestBatchRemoveResultDto>> RemoveGuestsFromEventAsync(
        Guid eventId,
        IReadOnlyCollection<Guid> guestIds)
    {
        var result = await eventRepository.RemoveGuestsFromEventAsync(eventId, guestIds);
        return result.Status switch
        {
            EventGuestChangeResult.NotFound => Result<EventGuestBatchRemoveResultDto>.Fail(
                new Error(ErrorCodes.NotFound, "Event not found")),
            EventGuestChangeResult.Unauthorized => Result<EventGuestBatchRemoveResultDto>.Fail(
                new Error(ErrorCodes.Forbidden, "Not allowed to modify this event")),
            _ => Result<EventGuestBatchRemoveResultDto>.Ok(result)
        };
    }

    private static Result<EventGuestChangeResult> MapGuestChangeResult(
        EventGuestChangeResult result,
        string action)
    {
        return result switch
        {
            EventGuestChangeResult.Added => Result<EventGuestChangeResult>.Ok(result),
            EventGuestChangeResult.Removed => Result<EventGuestChangeResult>.Ok(result),
            EventGuestChangeResult.Unauthorized => Result<EventGuestChangeResult>.Fail(
                new Error(ErrorCodes.Forbidden, "Not allowed to modify this event")),
            EventGuestChangeResult.NotFound => Result<EventGuestChangeResult>.Fail(
                new Error(ErrorCodes.NotFound, "Event or guest not found")),
            EventGuestChangeResult.AlreadyExists => Result<EventGuestChangeResult>.Fail(
                new Error(ErrorCodes.Conflict, $"Guest already in event; cannot {action}.")),
            EventGuestChangeResult.NotInEvent => Result<EventGuestChangeResult>.Fail(
                new Error(ErrorCodes.Conflict, $"Guest not in event; cannot {action}.")),
            _ => Result<EventGuestChangeResult>.Fail(
                new Error(ErrorCodes.Unexpected, "Unexpected event guest status"))
        };
    }
}
