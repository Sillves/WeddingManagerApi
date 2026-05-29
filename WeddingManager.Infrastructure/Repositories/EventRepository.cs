using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class EventRepository(WeddingDbContext context, IUserContextService userContextService) : IEventRepository
{
    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        var userId = userContextService.GetUserId();
        return await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .Where(a => a.Wedding.UserId == userId)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event?> GetByNameAsync(string name)
    {
        var userId = userContextService.GetUserId();
        return await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .Where(e => e.Wedding.UserId == userId)
            .FirstOrDefaultAsync(e => e.Name == name);
    }

    public async Task AddAsync(Event @event)
    {
        await context.Events.AddAsync(@event);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Event @event)
    {
        context.Events.Update(@event);
        await context.SaveChangesAsync();
    }

    public async Task<int> CountByWeddingIdAsync(Guid weddingId)
    {
        return await context.Events.CountAsync(e => e.WeddingId == weddingId);
    }

    public async Task DeleteAsync(Guid id)
    {
        var eventToDelete = await context.Events.FindAsync(id);
        if (eventToDelete == null)
        {
            return;
        }
        context.Events.Remove(eventToDelete);
        await context.SaveChangesAsync();
    }

    public async Task<EventGuestChangeResult> AddGuestToEventAsync(Guid eventId, Guid guestId)
    {
        var eventToUpdate = await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventToUpdate == null)
        {
            return EventGuestChangeResult.NotFound;
        }

        var userId = userContextService.GetUserId();
        if (eventToUpdate.Wedding.UserId != userId)
        {
            return EventGuestChangeResult.Unauthorized;
        }

        var guest = await context.Guests
            .FirstOrDefaultAsync(g => g.Id == guestId && g.WeddingId == eventToUpdate.WeddingId);
        if (guest == null)
        {
            return EventGuestChangeResult.NotFound;
        }
        if (eventToUpdate.Guests.Any(g => g.Id == guestId))
        {
            return EventGuestChangeResult.AlreadyExists;
        }

        eventToUpdate.Guests.Add(guest);
        await context.SaveChangesAsync();
        return EventGuestChangeResult.Added;
    }

    public async Task<EventGuestBatchChangeResultDto> AddGuestsToEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds)
    {
        var result = new EventGuestBatchChangeResultDto();

        var eventToUpdate = await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventToUpdate == null)
        {
            result.Status = EventGuestChangeResult.NotFound;
            return result;
        }

        var userId = userContextService.GetUserId();
        if (eventToUpdate.Wedding.UserId != userId)
        {
            result.Status = EventGuestChangeResult.Unauthorized;
            return result;
        }

        var distinctGuestIds = guestIds.Distinct().ToList();
        if (distinctGuestIds.Count == 0)
        {
            result.Status = EventGuestChangeResult.Added;
            return result;
        }

        var guests = await context.Guests
            .Where(g => distinctGuestIds.Contains(g.Id) && g.WeddingId == eventToUpdate.WeddingId)
            .ToListAsync();

        var foundGuestIdSet = guests.Select(g => g.Id).ToHashSet();
        foreach (var guestId in distinctGuestIds)
        {
            if (!foundGuestIdSet.Contains(guestId))
            {
                result.NotFoundGuestIds.Add(guestId);
            }
        }

        var existingGuestIdSet = eventToUpdate.Guests.Select(g => g.Id).ToHashSet();
        foreach (var guest in guests)
        {
            if (existingGuestIdSet.Contains(guest.Id))
            {
                result.AlreadyInEventGuestIds.Add(guest.Id);
                continue;
            }

            eventToUpdate.Guests.Add(guest);
            result.AddedGuestIds.Add(guest.Id);
        }

        if (result.AddedGuestIds.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        result.Status = EventGuestChangeResult.Added;
        return result;
    }

    public Task<EventGuestChangeResult> RemoveGuestFromEventAsync(Guid eventId, Guid guestId)
    {
        return RemoveGuestInternalAsync(eventId, guestId);
    }

    public async Task<EventGuestBatchRemoveResultDto> RemoveGuestsFromEventAsync(Guid eventId, IReadOnlyCollection<Guid> guestIds)
    {
        var result = new EventGuestBatchRemoveResultDto();

        var eventToUpdate = await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventToUpdate == null)
        {
            result.Status = EventGuestChangeResult.NotFound;
            return result;
        }

        var userId = userContextService.GetUserId();
        if (eventToUpdate.Wedding.UserId != userId)
        {
            result.Status = EventGuestChangeResult.Unauthorized;
            return result;
        }

        var distinctGuestIds = guestIds.Distinct().ToList();
        if (distinctGuestIds.Count == 0)
        {
            result.Status = EventGuestChangeResult.Removed;
            return result;
        }

        var eventGuestIdSet = eventToUpdate.Guests.Select(g => g.Id).ToHashSet();
        foreach (var guestId in distinctGuestIds)
        {
            if (!eventGuestIdSet.Contains(guestId))
            {
                result.NotInEventGuestIds.Add(guestId);
                continue;
            }

            var guest = eventToUpdate.Guests.First(g => g.Id == guestId);
            eventToUpdate.Guests.Remove(guest);
            result.RemovedGuestIds.Add(guestId);
        }

        if (result.RemovedGuestIds.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        result.Status = EventGuestChangeResult.Removed;
        return result;
    }

    public async Task<IEnumerable<Event>> GetByWeddingIdAsync(Guid weddingId)
    {
        var userId = userContextService.GetUserId();
        return await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .Where(e => e.WeddingId == weddingId && e.Wedding.UserId == userId)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByWeddingIdForPublicAsync(Guid weddingId)
    {
        return await context.Events
            .AsNoTracking()
            .Include(e => e.Guests)
            .Where(e => e.WeddingId == weddingId)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    private async Task<EventGuestChangeResult> RemoveGuestInternalAsync(Guid eventId, Guid guestId)
    {
        var eventToUpdate = await context.Events
            .Include(e => e.Guests)
            .Include(e => e.Wedding)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventToUpdate == null)
        {
            return EventGuestChangeResult.NotFound;
        }

        var userId = userContextService.GetUserId();
        if (eventToUpdate.Wedding.UserId != userId)
        {
            return EventGuestChangeResult.Unauthorized;
        }

        var guest = eventToUpdate.Guests.FirstOrDefault(g => g.Id == guestId);
        if (guest == null)
        {
            return EventGuestChangeResult.NotInEvent;
        }

        eventToUpdate.Guests.Remove(guest);
        await context.SaveChangesAsync();
        return EventGuestChangeResult.Removed;
    }
}
