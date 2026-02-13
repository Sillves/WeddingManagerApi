using System.Security.Cryptography;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingInvitationService(
    IWeddingInvitationRepository invitationRepository,
    IWeddingUserRepository weddingUserRepository,
    IWeddingRepository weddingRepository,
    IEmailService emailService) : IWeddingInvitationService
{
    public async Task<Result<WeddingInvitationDto>> CreateInvitationAsync(
        Guid weddingId, CreateWeddingInvitationRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Email))
        {
            return Result<WeddingInvitationDto>.Fail(
                new Error(ErrorCodes.Validation, "Email is required"));
        }

        var existing = await invitationRepository.GetPendingByEmailAsync(weddingId, requestDto.Email);
        if (existing != null)
        {
            return Result<WeddingInvitationDto>.Fail(
                new Error(ErrorCodes.Conflict, "A pending invitation already exists for this email"));
        }

        var invitation = new WeddingInvitation
        {
            Id = Guid.NewGuid(),
            WeddingId = weddingId,
            Email = requestDto.Email,
            Role = WeddingUserRole.Planner,
            CanAccessGuests = requestDto.CanAccessGuests,
            CanAccessEvents = requestDto.CanAccessEvents,
            CanAccessExpenses = requestDto.CanAccessExpenses,
            CanAccessWebsite = requestDto.CanAccessWebsite,
            IsReadOnly = requestDto.IsReadOnly,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
        };

        await invitationRepository.AddAsync(invitation);

        try
        {
            var wedding = await weddingRepository.GetByIdAsync(weddingId);
            if (wedding != null)
            {
                await emailService.SendPlannerInvitationAsync(invitation, wedding);
            }
        }
        catch
        {
            // Email failure shouldn't block invitation creation
        }

        return Result<WeddingInvitationDto>.Ok(MapToDto(invitation));
    }

    public async Task<Result<IEnumerable<WeddingInvitationDto>>> GetInvitationsAsync(Guid weddingId)
    {
        var invitations = await invitationRepository.GetByWeddingIdAsync(weddingId);
        return Result<IEnumerable<WeddingInvitationDto>>.Ok(invitations.Select(MapToDto));
    }

    public async Task<Result> CancelInvitationAsync(Guid weddingId, Guid invitationId)
    {
        var invitation = await invitationRepository.GetByIdAsync(invitationId);
        if (invitation == null || invitation.WeddingId != weddingId)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "Invitation not found"));
        }

        if (invitation.AcceptedAt != null)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Cannot cancel an accepted invitation"));
        }

        await invitationRepository.DeleteAsync(invitationId);
        return Result.Ok();
    }

    public async Task<Result<WeddingInvitationDto>> GetByTokenAsync(string token)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token);
        if (invitation == null)
        {
            return Result<WeddingInvitationDto>.Fail(
                new Error(ErrorCodes.NotFound, "Invitation not found"));
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            return Result<WeddingInvitationDto>.Fail(
                new Error(ErrorCodes.Validation, "Invitation has expired"));
        }

        if (invitation.AcceptedAt != null)
        {
            return Result<WeddingInvitationDto>.Fail(
                new Error(ErrorCodes.Validation, "Invitation has already been accepted"));
        }

        return Result<WeddingInvitationDto>.Ok(MapToDto(invitation));
    }

    public async Task<Result> AcceptInvitationAsync(string token, Guid userId)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token);
        if (invitation == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "Invitation not found"));
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Invitation has expired"));
        }

        if (invitation.AcceptedAt != null)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Invitation has already been accepted"));
        }

        var existingUser = await weddingUserRepository.GetByIdAsync(invitation.WeddingId, userId);
        if (existingUser != null)
        {
            return Result.Fail(new Error(ErrorCodes.Conflict, "You are already a member of this wedding"));
        }

        var weddingUser = new WeddingUser
        {
            WeddingId = invitation.WeddingId,
            UserId = userId,
            Role = invitation.Role,
            AddedAt = DateTime.UtcNow,
            CanAccessGuests = invitation.CanAccessGuests,
            CanAccessEvents = invitation.CanAccessEvents,
            CanAccessExpenses = invitation.CanAccessExpenses,
            CanAccessWebsite = invitation.CanAccessWebsite,
            IsReadOnly = invitation.IsReadOnly,
        };

        await weddingUserRepository.AddAsync(weddingUser);

        invitation.AcceptedAt = DateTime.UtcNow;
        await invitationRepository.UpdateAsync(invitation);

        return Result.Ok();
    }

    private static string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static WeddingInvitationDto MapToDto(WeddingInvitation invitation)
    {
        return new WeddingInvitationDto
        {
            Id = invitation.Id,
            WeddingId = invitation.WeddingId,
            Email = invitation.Email,
            Role = invitation.Role,
            CanAccessGuests = invitation.CanAccessGuests,
            CanAccessEvents = invitation.CanAccessEvents,
            CanAccessExpenses = invitation.CanAccessExpenses,
            CanAccessWebsite = invitation.CanAccessWebsite,
            IsReadOnly = invitation.IsReadOnly,
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = invitation.AcceptedAt,
            CreatedAt = invitation.CreatedAt,
        };
    }
}
