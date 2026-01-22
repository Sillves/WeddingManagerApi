using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class WeddingUserRepository(WeddingDbContext context) : IWeddingUserRepository
{
    public async Task<WeddingUser?> GetByIdAsync(Guid weddingId, Guid userId)
    {
        return await context.WeddingUsers
            .Include(wu => wu.User)
            .Include(wu => wu.Wedding)
            .FirstOrDefaultAsync(wu => wu.WeddingId == weddingId && wu.UserId == userId);
    }

    public async Task<IEnumerable<WeddingUser>> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.WeddingUsers
            .Where(wu => wu.WeddingId == weddingId)
            .Include(wu => wu.User)
            .ToListAsync();
    }

    public async Task AddAsync(WeddingUser weddingUser)
    {
        await context.WeddingUsers.AddAsync(weddingUser);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid weddingId, Guid userId)
    {
        var weddingUser = await context.WeddingUsers.FindAsync(weddingId, userId);
        if (weddingUser != null)
        {
            context.WeddingUsers.Remove(weddingUser);
            await context.SaveChangesAsync();
        }
    }
}
