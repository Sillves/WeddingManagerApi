
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IWeddingUserService
{
    Task<Result<WeddingUserDto>> AddUserToWeddingAsync(Guid weddingId, AddWeddingUserRequestDto requestDto);
    Task<Result<IEnumerable<WeddingUserDto>>> GetWeddingUsersAsync(Guid weddingId);
    Task<Result<WeddingUserDto>> UpdatePermissionsAsync(Guid weddingId, Guid userId, UpdateWeddingUserRequestDto requestDto);
    Task<Result> RemoveUserFromWeddingAsync(Guid weddingId, Guid userId);
}
