
using WeddingManager.Domain.DTO;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingUserService
{
    Task<WeddingUserDto> AddUserToWeddingAsync(Guid weddingId, AddWeddingUserRequestDto requestDto);
    Task<IEnumerable<WeddingUserDto>> GetWeddingUsersAsync(Guid weddingId);
    Task RemoveUserFromWeddingAsync(Guid weddingId, Guid userId);
}
