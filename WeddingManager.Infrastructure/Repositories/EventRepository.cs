using Microsoft.EntityFrameworkCore;
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

    public Task<EventGuestChangeResult> RemoveGuestFromEventAsync(Guid eventId, Guid guestId)
    {
        return RemoveGuestInternalAsync(eventId, guestId);
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
