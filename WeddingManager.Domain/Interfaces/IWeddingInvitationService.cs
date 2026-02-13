using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingInvitationService
{
    Task<Result<WeddingInvitationDto>> CreateInvitationAsync(Guid weddingId, CreateWeddingInvitationRequestDto requestDto);
    Task<Result<IEnumerable<WeddingInvitationDto>>> GetInvitationsAsync(Guid weddingId);
    Task<Result> CancelInvitationAsync(Guid weddingId, Guid invitationId);
    Task<Result<WeddingInvitationDto>> GetByTokenAsync(string token);
    Task<Result> AcceptInvitationAsync(string token, Guid userId);
}
