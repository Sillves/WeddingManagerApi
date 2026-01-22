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

    public async Task<Wedding?> GetByIdAsync(Guid id)
    {
        return await context.Weddings
            .Include(w => w.User)
            .Include(w => w.Guests)
            .Include(w => w.Pages)
            .Include(w => w.Media)
            .FirstOrDefaultAsync(w => w.Id == id);
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
