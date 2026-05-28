using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IInvitationFlowRepository
{
    Task<IEnumerable<InvitationFlow>> GetByWeddingIdAsync(Guid weddingId);
    Task<InvitationFlow?> GetByIdAsync(Guid flowId);
    Task<InvitationFlow?> GetByPasscodeAsync(Guid weddingId, string passcode);
    Task AddAsync(InvitationFlow flow);
    Task UpdateAsync(InvitationFlow flow);
    Task DeleteAsync(Guid flowId);
}
