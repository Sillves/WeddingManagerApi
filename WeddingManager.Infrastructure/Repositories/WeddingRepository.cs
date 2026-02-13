using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class WeddingRepository(WeddingDbContext context) : IWeddingRepository
{
    public async Task<IEnumerable<Wedding>> GetAllAsync(Guid userId)
    {
        return await context.Weddings
            .Where(w => w.UserId == userId)
            .Include(w => w.User) // Include navigation properties
            .ToListAsync();
    }

    public async Task<IEnumerable<(Wedding Wedding, WeddingUser WeddingUser)>> GetAllWithRoleAsync(Guid userId)
    {
        var weddingUsers = await context.WeddingUsers
            .Where(wu => wu.UserId == userId)
            .Include(wu => wu.Wedding)
                .ThenInclude(w => w.Guests)
            .ToListAsync();

        return weddingUsers.Select(wu => (wu.Wedding, wu));
    }

    public async Task<Wedding?> GetByIdAsync(Guid id)
    {
        return await context.Weddings
            .Include(w => w.User)
            .Include(w => w.Guests)
            .Include(w => w.Pages)
            .Include(w => w.Media)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Wedding?> GetByIdOrSlugAsync(string idOrSlug)
    {
        var query = context.Weddings.AsNoTracking();
        if (Guid.TryParse(idOrSlug, out var id))
        {
            return await query.FirstOrDefaultAsync(w => w.Id == id);
        }

        return await query.FirstOrDefaultAsync(w => w.Slug == idOrSlug);
    }

    public async Task<IEnumerable<Wedding>> GetWeddingsWithMediaOlderThanAsync(DateTime cutoffDate)
    {
        return await context.Weddings
            .Include(w => w.Media)
            .Where(w => w.Date < cutoffDate && w.Media.Any())
            .ToListAsync();
    }

    public async Task AddAsync(Wedding wedding)
    {
        await context.Weddings.AddAsync(wedding);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Wedding wedding)
    {
        context.Weddings.Update(wedding);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var wedding = await context.Weddings.FindAsync(id);
        if (wedding != null)
        {
            context.Weddings.Remove(wedding);
            await context.SaveChangesAsync();
        }
    }
}
