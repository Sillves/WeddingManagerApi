using WeddingManager.Domain.DTO;

namespace WeddingManager.Domain.Interfaces;

public interface IGuestService
{
    Task<GuestDto?> GetByIdAsync(Guid guestId);
    Task<IEnumerable<GuestDto>> GetByWeddingIdAsync(Guid weddingId);
    Task<GuestDto> CreateGuestAsync(Guid weddingId, CreateGuestRequestDto requestDto);
    Task<GuestDto> UpdateGuestAsync(Guid guestId, UpdateGuestRequestDto requestDto);
    Task DeleteGuestAsync(Guid guestId);
}
