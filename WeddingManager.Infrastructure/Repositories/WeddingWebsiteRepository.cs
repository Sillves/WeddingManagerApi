using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class WeddingWebsiteRepository(WeddingDbContext context) : IWeddingWebsiteRepository
{
    public async Task<WeddingWebsite?> GetByIdAsync(Guid id)
    {
        return await context.WeddingWebsites
            .Include(w => w.Wedding)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<WeddingWebsite?> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.WeddingWebsites
            .Include(w => w.Wedding)
            .FirstOrDefaultAsync(w => w.WeddingId == weddingId);
    }

    public async Task<WeddingWebsite?> GetPublishedBySlugAsync(string slug)
    {
        return await context.WeddingWebsites
            .Include(w => w.Wedding)
            .ThenInclude(w => w.Events)
            .Where(w => w.IsPublished && w.Wedding.Slug == slug)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(WeddingWebsite website)
    {
        await context.WeddingWebsites.AddAsync(website);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WeddingWebsite website)
    {
        website.UpdatedAt = DateTime.UtcNow;
        context.WeddingWebsites.Update(website);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var website = await context.WeddingWebsites.FindAsync(id);
        if (website != null)
        {
            context.WeddingWebsites.Remove(website);
            await context.SaveChangesAsync();
        }
    }
}
