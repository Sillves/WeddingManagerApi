
using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Services;

public class WeddingUserService(
    IWeddingUserRepository weddingUserRepository,
    IMapper mapper)
    : IWeddingUserService
{
    public async Task<WeddingUserDto> AddUserToWeddingAsync(Guid weddingId, AddWeddingUserRequestDto requestDto)
    {
        var existingUser = await weddingUserRepository.GetByIdAsync(weddingId, requestDto.UserId);
        if (existingUser != null)
            throw new InvalidOperationException("User is already added to this wedding");

        var weddingUser = new WeddingUser
        {
            WeddingId = weddingId,
            UserId = requestDto.UserId,
            Role = requestDto.Role,
            AddedAt = DateTime.UtcNow
        };

        await weddingUserRepository.AddAsync(weddingUser);
        return mapper.Map<WeddingUserDto>(weddingUser);
    }

    public async Task<IEnumerable<WeddingUserDto>> GetWeddingUsersAsync(Guid weddingId)
    {
        var users = await weddingUserRepository.GetByWeddingIdAsync(weddingId);
        return mapper.Map<IEnumerable<WeddingUserDto>>(users);
    }

    public async Task RemoveUserFromWeddingAsync(Guid weddingId, Guid userId)
    {
        var weddingUser = await weddingUserRepository.GetByIdAsync(weddingId, userId);
        if (weddingUser == null)
            throw new ArgumentException("User is not part of this wedding");

        await weddingUserRepository.DeleteAsync(weddingId, userId);
    }
}
