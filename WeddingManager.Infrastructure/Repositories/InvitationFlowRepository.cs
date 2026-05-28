using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class InvitationFlowRepository(WeddingDbContext context) : IInvitationFlowRepository
{
    public async Task<IEnumerable<InvitationFlow>> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.InvitationFlows
            .Where(f => f.WeddingId == weddingId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<InvitationFlow?> GetByIdAsync(Guid flowId)
    {
        return await context.InvitationFlows
            .Include(f => f.Wedding)
            .FirstOrDefaultAsync(f => f.Id == flowId);
    }

    public async Task<InvitationFlow?> GetByPasscodeAsync(Guid weddingId, string passcode)
    {
        return await context.InvitationFlows
            .FirstOrDefaultAsync(f => f.WeddingId == weddingId && f.Passcode == passcode);
    }

    public async Task AddAsync(InvitationFlow flow)
    {
        await context.InvitationFlows.AddAsync(flow);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(InvitationFlow flow)
    {
        context.InvitationFlows.Update(flow);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid flowId)
    {
        var flow = await context.InvitationFlows.FindAsync(flowId);
        if (flow != null)
        {
            context.InvitationFlows.Remove(flow);
            await context.SaveChangesAsync();
        }
    }
}
