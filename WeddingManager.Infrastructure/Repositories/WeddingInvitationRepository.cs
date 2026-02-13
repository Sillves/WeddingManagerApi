using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class WeddingInvitationRepository(WeddingDbContext context) : IWeddingInvitationRepository
{
    public async Task<WeddingInvitation?> GetByIdAsync(Guid id)
    {
        return await context.WeddingInvitations
            .Include(wi => wi.Wedding)
            .FirstOrDefaultAsync(wi => wi.Id == id);
    }

    public async Task<WeddingInvitation?> GetByTokenAsync(string token)
    {
        return await context.WeddingInvitations
            .Include(wi => wi.Wedding)
                .ThenInclude(w => w.User)
            .FirstOrDefaultAsync(wi => wi.Token == token);
    }

    public async Task<IEnumerable<WeddingInvitation>> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.WeddingInvitations
            .Where(wi => wi.WeddingId == weddingId)
            .OrderByDescending(wi => wi.CreatedAt)
            .ToListAsync();
    }

    public async Task<WeddingInvitation?> GetPendingByEmailAsync(Guid weddingId, string email)
    {
        return await context.WeddingInvitations
            .FirstOrDefaultAsync(wi =>
                wi.WeddingId == weddingId &&
                wi.Email == email &&
                wi.AcceptedAt == null &&
                wi.ExpiresAt > DateTime.UtcNow);
    }

    public async Task AddAsync(WeddingInvitation invitation)
    {
        await context.WeddingInvitations.AddAsync(invitation);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WeddingInvitation invitation)
    {
        context.WeddingInvitations.Update(invitation);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var invitation = await context.WeddingInvitations.FindAsync(id);
        if (invitation != null)
        {
            context.WeddingInvitations.Remove(invitation);
            await context.SaveChangesAsync();
        }
    }
}
