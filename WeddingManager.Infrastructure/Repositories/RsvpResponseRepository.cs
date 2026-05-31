using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class RsvpResponseRepository(WeddingDbContext context) : IRsvpResponseRepository
{
    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByWeddingAsync(Guid weddingId)
    {
        var counts = await context.RsvpResponses
            .Where(r => r.WeddingId == weddingId)
            .GroupBy(r => r.InvitationFlowId)
            .Select(g => new { FlowId = g.Key, Count = g.Count() })
            .ToListAsync();
        return counts.ToDictionary(c => c.FlowId, c => c.Count);
    }

    public async Task<int> CountByFlowAsync(Guid flowId)
    {
        return await context.RsvpResponses.CountAsync(r => r.InvitationFlowId == flowId);
    }

    public async Task<IEnumerable<RsvpResponse>> GetByWeddingIdAsync(Guid weddingId, Guid? flowId)
    {
        var query = context.RsvpResponses
            .Include(r => r.Guest)
            .Include(r => r.InvitationFlow)
            .Where(r => r.WeddingId == weddingId);

        if (flowId.HasValue)
        {
            query = query.Where(r => r.InvitationFlowId == flowId.Value);
        }

        return await query
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();
    }

    public async Task<bool> SubmitAsync(IReadOnlyCollection<Guest> guests, IReadOnlyCollection<RsvpResponse> responses)
    {
        await context.Guests.AddRangeAsync(guests);
        await context.RsvpResponses.AddRangeAsync(responses);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Unique (dedupe) violation — clear the failed change tracker so the context is reusable.
            foreach (var entry in context.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }
            return false;
        }
    }
}
