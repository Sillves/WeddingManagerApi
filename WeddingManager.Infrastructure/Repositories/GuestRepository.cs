using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class GuestRepository(WeddingDbContext context) : IGuestRepository
{
    public async Task<Guest?> GetByIdAsync(Guid id)
    {
        return await context.Guests
            .Include(g => g.Wedding)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<IEnumerable<Guest>> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.Guests
            .Where(g => g.WeddingId == weddingId)
            .ToListAsync();
    }

    public async Task<Guest?> GetByEmailAsync(Guid weddingId, string email)
    {
        return await context.Guests
            .FirstOrDefaultAsync(g => g.WeddingId == weddingId && g.Email == email);
    }

    public async Task AddAsync(Guest guest)
    {
        await context.Guests.AddAsync(guest);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Guest guest)
    {
        context.Guests.Update(guest);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var guest = await context.Guests.FindAsync(id);
        if (guest != null)
        {
            context.Guests.Remove(guest);
            await context.SaveChangesAsync();
        }
    }
}
