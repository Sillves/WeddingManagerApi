using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IInvitationFlowService
{
    Task<Result<IEnumerable<InvitationFlowDto>>> GetByWeddingIdAsync(Guid weddingId);
    Task<Result<InvitationFlowDto>> CreateAsync(Guid weddingId, CreateInvitationFlowRequestDto requestDto);
    Task<Result<InvitationFlowDto>> UpdateAsync(Guid weddingId, Guid flowId, UpdateInvitationFlowRequestDto requestDto);
    Task<Result> DeleteAsync(Guid weddingId, Guid flowId, bool force);
}
