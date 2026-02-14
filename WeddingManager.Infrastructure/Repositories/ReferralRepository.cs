using Microsoft.EntityFrameworkCore;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;

namespace WeddingManager.Infrastructure.Repositories;

public class ReferralRepository(WeddingDbContext context) : IReferralRepository
{
    public async Task<IEnumerable<Referral>> GetByReferrerUserIdAsync(Guid userId)
    {
        return await context.Referrals
            .Where(r => r.ReferrerUserId == userId)
            .Include(r => r.ReferredUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Referral?> GetByReferredUserIdAsync(Guid userId)
    {
        return await context.Referrals
            .FirstOrDefaultAsync(r => r.ReferredUserId == userId);
    }

    public async Task AddAsync(Referral referral)
    {
        await context.Referrals.AddAsync(referral);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Referral referral)
    {
        context.Referrals.Update(referral);
        await context.SaveChangesAsync();
    }
}
