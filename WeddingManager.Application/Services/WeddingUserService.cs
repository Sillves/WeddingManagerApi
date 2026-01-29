
using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingUserService(
    IWeddingUserRepository weddingUserRepository,
    IMapper mapper)
    : IWeddingUserService
{
    public async Task<Result<WeddingUserDto>> AddUserToWeddingAsync(Guid weddingId, AddWeddingUserRequestDto requestDto)
    {
        var existingUser = await weddingUserRepository.GetByIdAsync(weddingId, requestDto.UserId);
        if (existingUser != null)
        {
            return Result<WeddingUserDto>.Fail(
                new Error(ErrorCodes.Conflict, "User is already added to this wedding"));
        }

        var weddingUser = new WeddingUser
        {
            WeddingId = weddingId,
            UserId = requestDto.UserId,
            Role = requestDto.Role,
            AddedAt = DateTime.UtcNow
        };

        await weddingUserRepository.AddAsync(weddingUser);
        return Result<WeddingUserDto>.Ok(mapper.Map<WeddingUserDto>(weddingUser));
    }

    public async Task<Result<IEnumerable<WeddingUserDto>>> GetWeddingUsersAsync(Guid weddingId)
    {
        var users = await weddingUserRepository.GetByWeddingIdAsync(weddingId);
        return Result<IEnumerable<WeddingUserDto>>.Ok(mapper.Map<IEnumerable<WeddingUserDto>>(users));
    }

    public async Task<Result> RemoveUserFromWeddingAsync(Guid weddingId, Guid userId)
    {
        var weddingUser = await weddingUserRepository.GetByIdAsync(weddingId, userId);
        if (weddingUser == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "User is not part of this wedding"));
        }

        await weddingUserRepository.DeleteAsync(weddingId, userId);
        return Result.Ok();
    }
}
