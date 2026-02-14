using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IReferralRepository
{
    Task<IEnumerable<Referral>> GetByReferrerUserIdAsync(Guid userId);
    Task<Referral?> GetByReferredUserIdAsync(Guid userId);
    Task AddAsync(Referral referral);
    Task UpdateAsync(Referral referral);
}
