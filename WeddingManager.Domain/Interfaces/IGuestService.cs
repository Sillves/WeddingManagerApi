using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IGuestService
{
    Task<Result<GuestDto>> GetByIdAsync(Guid guestId);
    Task<Result<IEnumerable<GuestDto>>> GetByWeddingIdAsync(Guid weddingId);
    Task<Result<GuestDto>> CreateGuestAsync(Guid weddingId, CreateGuestRequestDto requestDto);
    Task<Result<GuestDto>> UpdateGuestAsync(Guid guestId, UpdateGuestRequestDto requestDto);
    Task<Result<GuestDto>> SubmitRsvpAsync(Guid weddingId, RsvpSubmitRequestDto requestDto);
    Task<Result> SendInvitationAsync(Guid weddingId, Guid guestId);
    Task<Result<InvitationSendResultDto>> SendInvitationsAsync(Guid weddingId, IReadOnlyCollection<Guid>? guestIds);
    Task<Result> DeleteGuestAsync(Guid guestId);
}
