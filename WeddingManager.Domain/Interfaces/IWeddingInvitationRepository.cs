using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingInvitationRepository
{
    Task<WeddingInvitation?> GetByIdAsync(Guid id);
    Task<WeddingInvitation?> GetByTokenAsync(string token);
    Task<IEnumerable<WeddingInvitation>> GetByWeddingIdAsync(Guid weddingId);
    Task<WeddingInvitation?> GetPendingByEmailAsync(Guid weddingId, string email);
    Task AddAsync(WeddingInvitation invitation);
    Task UpdateAsync(WeddingInvitation invitation);
    Task DeleteAsync(Guid id);
}
