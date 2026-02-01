using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class MediaRepository(WeddingDbContext context) : IMediaRepository
{
    public async Task<Media?> GetByIdAsync(Guid id)
    {
        return await context.Media
            .Include(m => m.Wedding)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Media>> GetByWeddingIdAsync(Guid weddingId)
    {
        return await context.Media
            .Where(m => m.WeddingId == weddingId)
            .OrderByDescending(m => m.UploadedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Media media)
    {
        await context.Media.AddAsync(media);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var media = await context.Media.FindAsync(id);
        if (media != null)
        {
            context.Media.Remove(media);
            await context.SaveChangesAsync();
        }
    }
}
