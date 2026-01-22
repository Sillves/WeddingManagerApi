using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class WeddingRepository : IWeddingRepository
{
    private readonly WeddingDbContext _context;

    public WeddingRepository(WeddingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Wedding>> GetAllAsync(Guid userId)
    {
        return await _context.Weddings
            .Where(w => w.UserId == userId)
            .Include(w => w.User) // Include navigation properties
            .ToListAsync();
    }

    public async Task<Wedding?> GetByIdAsync(Guid id)
    {
        return await _context.Weddings
            .Include(w => w.User)
            .Include(w => w.Guests)
            .Include(w => w.Pages)
            .Include(w => w.Media)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task AddAsync(Wedding wedding)
    {
        await _context.Weddings.AddAsync(wedding);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Wedding wedding)
    {
        _context.Weddings.Update(wedding);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var wedding = await _context.Weddings.FindAsync(id);
        if (wedding != null)
        {
            _context.Weddings.Remove(wedding);
            await _context.SaveChangesAsync();
        }
    }
}
