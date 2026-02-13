using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingUserRepository
{
    Task<WeddingUser?> GetByIdAsync(Guid weddingId, Guid userId);
    Task<IEnumerable<WeddingUser>> GetByWeddingIdAsync(Guid weddingId);
    Task AddAsync(WeddingUser weddingUser);
    Task UpdateAsync(WeddingUser weddingUser);
    Task DeleteAsync(Guid weddingId, Guid userId);
}
