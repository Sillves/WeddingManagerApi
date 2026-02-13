using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingUserService(
    IWeddingUserRepository weddingUserRepository,
    ApplicationMapper mapper)
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
            AddedAt = DateTime.UtcNow,
            CanAccessGuests = requestDto.CanAccessGuests,
            CanAccessEvents = requestDto.CanAccessEvents,
            CanAccessExpenses = requestDto.CanAccessExpenses,
            CanAccessWebsite = requestDto.CanAccessWebsite,
            IsReadOnly = requestDto.IsReadOnly,
        };

        await weddingUserRepository.AddAsync(weddingUser);
        return Result<WeddingUserDto>.Ok(mapper.WeddingUserToDto(weddingUser));
    }

    public async Task<Result<IEnumerable<WeddingUserDto>>> GetWeddingUsersAsync(Guid weddingId)
    {
        var users = await weddingUserRepository.GetByWeddingIdAsync(weddingId);
        return Result<IEnumerable<WeddingUserDto>>.Ok(mapper.WeddingUsersToDto(users));
    }

    public async Task<Result<WeddingUserDto>> UpdatePermissionsAsync(Guid weddingId, Guid userId, UpdateWeddingUserRequestDto requestDto)
    {
        var weddingUser = await weddingUserRepository.GetByIdAsync(weddingId, userId);
        if (weddingUser == null)
        {
            return Result<WeddingUserDto>.Fail(
                new Error(ErrorCodes.NotFound, "User is not part of this wedding"));
        }

        if (weddingUser.Role == WeddingUserRole.Owner)
        {
            return Result<WeddingUserDto>.Fail(
                new Error(ErrorCodes.Forbidden, "Cannot modify Owner permissions"));
        }

        weddingUser.CanAccessGuests = requestDto.CanAccessGuests;
        weddingUser.CanAccessEvents = requestDto.CanAccessEvents;
        weddingUser.CanAccessExpenses = requestDto.CanAccessExpenses;
        weddingUser.CanAccessWebsite = requestDto.CanAccessWebsite;
        weddingUser.IsReadOnly = requestDto.IsReadOnly;

        await weddingUserRepository.UpdateAsync(weddingUser);
        return Result<WeddingUserDto>.Ok(mapper.WeddingUserToDto(weddingUser));
    }

    public async Task<Result> RemoveUserFromWeddingAsync(Guid weddingId, Guid userId)
    {
        var weddingUser = await weddingUserRepository.GetByIdAsync(weddingId, userId);
        if (weddingUser == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "User is not part of this wedding"));
        }

        if (weddingUser.Role == WeddingUserRole.Owner)
        {
            return Result.Fail(new Error(ErrorCodes.Forbidden, "Cannot remove the wedding Owner"));
        }

        await weddingUserRepository.DeleteAsync(weddingId, userId);
        return Result.Ok();
    }
}
