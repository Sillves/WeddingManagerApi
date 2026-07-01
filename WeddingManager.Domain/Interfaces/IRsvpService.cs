using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IRsvpService
{
    Task<Result<RsvpFlowStateDto>> GetFlowStateAsync(string weddingSlugOrId, Guid? unlockedFlowId);
    Task<Result<RsvpFlowPublicDto>> UnlockAsync(string weddingSlugOrId, string passcode);
    Task<Result<RsvpSubmitResultDto>> SubmitAsync(string weddingSlugOrId, Guid flowId, RsvpFlowSubmitRequestDto requestDto);

    /// <summary>Couple-facing dashboard read.</summary>
    Task<Result<IEnumerable<RsvpResponseDto>>> GetResponsesAsync(Guid weddingId, Guid? flowId);
}
