
using AutoMapper;
using Microsoft.Extensions.Logging;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class GuestService(
    IGuestRepository guestRepository,
    IWeddingRepository weddingRepository,
    IEmailService emailService,
    ISubscriptionLimitService subscriptionLimitService,
    IMapper mapper,
    ILogger<GuestService> logger)
    : IGuestService
{
    private const string DefaultLanguage = "en";
    private static readonly TimeSpan InvitationTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<GuestDto>> GetByIdAsync(Guid guestId)
    {
        var guest = await guestRepository.GetByIdAsync(guestId);
        return guest == null
            ? Result<GuestDto>.Fail(new Error(ErrorCodes.NotFound, $"Guest with id {guestId} not found"))
            : Result<GuestDto>.Ok(mapper.Map<GuestDto>(guest));
    }

    public async Task<Result<IEnumerable<GuestDto>>> GetByWeddingIdAsync(Guid weddingId)
    {
        var guests = await guestRepository.GetByWeddingIdAsync(weddingId);
        return Result<IEnumerable<GuestDto>>.Ok(mapper.Map<IEnumerable<GuestDto>>(guests));
    }

    public async Task<Result<GuestDto>> CreateGuestAsync(Guid weddingId, CreateGuestRequestDto requestDto)
    {
        var validationResult = GuestValidation.ValidateInput(requestDto);
        if (!validationResult.IsSuccess)
        {
            return Result<GuestDto>.Fail(validationResult.Errors);
        }

        var limitResult = await subscriptionLimitService.EnsureGuestLimitAsync(weddingId);
        if (!limitResult.IsSuccess)
        {
            return Result<GuestDto>.Fail(limitResult.Errors);
        }

        var existingGuest = await guestRepository.GetByEmailAsync(weddingId, requestDto.Email);
        if (existingGuest != null)
        {
            return Result<GuestDto>.Fail(
                new Error(ErrorCodes.Conflict,
                    $"A guest with email {requestDto.Email} already exists for this wedding"));
        }

        var guest = new Guest
        {
            Id = Guid.NewGuid(),
            Name = requestDto.Name,
            Email = requestDto.Email,
            RsvpStatus = requestDto.RsvpStatus,
            PreferredLanguage = NormalizeLanguage(requestDto.PreferredLanguage),
            WeddingId = weddingId
        };

        await guestRepository.AddAsync(guest);
        return Result<GuestDto>.Ok(mapper.Map<GuestDto>(guest));
    }

    public async Task<Result<GuestDto>> UpdateGuestAsync(Guid guestId, UpdateGuestRequestDto requestDto)
    {
        var guest = await guestRepository.GetByIdAsync(guestId);
        if (guest == null)
        {
            return Result<GuestDto>.Fail(new Error(ErrorCodes.NotFound, $"Guest with id {guestId} not found"));
        }

        var validationResult = GuestValidation.ValidateInput(requestDto);
        if (!validationResult.IsSuccess)
        {
            return Result<GuestDto>.Fail(validationResult.Errors);
        }

        if (guest.Email != requestDto.Email)
        {
            var existingGuest = await guestRepository.GetByEmailAsync(guest.WeddingId, requestDto.Email);
            if (existingGuest != null)
            {
                return Result<GuestDto>.Fail(
                    new Error(ErrorCodes.Conflict,
                        $"A guest with email {requestDto.Email} already exists for this wedding"));
            }
        }

        guest.Name = requestDto.Name;
        guest.Email = requestDto.Email;
        guest.RsvpStatus = requestDto.RsvpStatus;
        guest.PreferredLanguage = NormalizeLanguage(requestDto.PreferredLanguage);

        await guestRepository.UpdateAsync(guest);
        return Result<GuestDto>.Ok(mapper.Map<GuestDto>(guest));
    }

    public async Task<Result> DeleteGuestAsync(Guid guestId)
    {
        var guest = await guestRepository.GetByIdAsync(guestId);
        if (guest == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, $"Guest with id {guestId} not found"));
        }

        await guestRepository.DeleteAsync(guestId);
        return Result.Ok();
    }

    public async Task<Result<GuestDto>> SubmitRsvpAsync(Guid weddingId, RsvpSubmitRequestDto requestDto)
    {
        var validationResult = GuestValidation.ValidateInput(requestDto);
        if (!validationResult.IsSuccess)
        {
            return Result<GuestDto>.Fail(validationResult.Errors);
        }

        var guest = await guestRepository.GetByEmailAsync(weddingId, requestDto.Email);
        if (guest == null)
        {
            return Result<GuestDto>.Fail(
                new Error(ErrorCodes.NotFound, "Guest not found for the provided email"));
        }

        guest.Name = requestDto.Name;
        guest.RsvpStatus = requestDto.RsvpStatus;

        await guestRepository.UpdateAsync(guest);

        // var wedding = await weddingRepository.GetByIdAsync(weddingId);
        // if (wedding != null)
        // {
        //     await emailService.SendRsvpConfirmationAsync(guest, wedding);
        // }

        return Result<GuestDto>.Ok(mapper.Map<GuestDto>(guest));
    }

    public async Task<Result> SendInvitationAsync(Guid weddingId, Guid guestId)
    {
        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"));
        }

        var guest = await guestRepository.GetByIdAsync(guestId);
        if (guest == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "Guest not found"));
        }

        if (guest.WeddingId != weddingId)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Guest does not belong to this wedding"));
        }

        var limitResult = await subscriptionLimitService.EnsureEmailLimitAsync(weddingId, 1);
        if (!limitResult.IsSuccess)
        {
            return Result.Fail(limitResult.Errors);
        }

        await EnsureInvitationTokenAsync(guest);

        try
        {
            await emailService.SendInvitationAsync(guest, wedding);
            guest.InvitationSentAt = DateTime.UtcNow;
            await guestRepository.UpdateAsync(guest);
            var recordResult = await subscriptionLimitService.RecordEmailsSentAsync(wedding.UserId, 1);
            if (!recordResult.IsSuccess)
            {
                return Result.Fail(recordResult.Errors);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invitation to guest {GuestId} for wedding {WeddingId}", guestId, weddingId);
            return Result.Fail(new Error(ErrorCodes.ExternalFailure, "Failed to send invitation email"));
        }

        return Result.Ok();
    }

    public async Task<Result<InvitationSendResultDto>> SendInvitationsAsync(
        Guid weddingId,
        IReadOnlyCollection<Guid>? guestIds)
    {
        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding == null)
        {
            return Result<InvitationSendResultDto>.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"));
        }

        List<Guest> guests;
        if (guestIds is { Count: > 0 })
        {
            guests = (await guestRepository.GetByIdsAsync(weddingId, guestIds)).ToList();
            var missingIds = guestIds.Except(guests.Select(g => g.Id)).ToList();
            if (missingIds.Count > 0)
            {
                return Result<InvitationSendResultDto>.Fail(
                    new Error(ErrorCodes.Validation, $"Guests not found: {string.Join(", ", missingIds)}"));
            }
        }
        else
        {
            guests = (await guestRepository.GetByWeddingIdAsync(weddingId))
                .Where(g => g.RsvpStatus == RsvpStatus.Pending)
                .ToList();
        }

        if (guests.Count == 0)
        {
            return Result<InvitationSendResultDto>.Ok(new InvitationSendResultDto());
        }

        var limitResult = await subscriptionLimitService.EnsureEmailLimitAsync(weddingId, guests.Count);
        if (!limitResult.IsSuccess)
        {
            return Result<InvitationSendResultDto>.Fail(limitResult.Errors);
        }

        var result = new InvitationSendResultDto();
        var sentForUsage = 0;

        foreach (var guest in guests)
        {
            try
            {
                await EnsureInvitationTokenAsync(guest);
                await emailService.SendInvitationAsync(guest, wedding);
                guest.InvitationSentAt = DateTime.UtcNow;
                await guestRepository.UpdateAsync(guest);
                result.SentCount++;
                sentForUsage++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invitation to guest {GuestId} for wedding {WeddingId}", guest.Id, weddingId);
                result.FailedCount++;
                result.FailedGuestIds.Add(guest.Id);
            }
        }

        if (sentForUsage > 0)
        {
            var recordResult = await subscriptionLimitService.RecordEmailsSentAsync(wedding.UserId, sentForUsage);
            if (!recordResult.IsSuccess)
            {
                return Result<InvitationSendResultDto>.Fail(recordResult.Errors);
            }
        }

        return Result<InvitationSendResultDto>.Ok(result);
    }

    private async Task EnsureInvitationTokenAsync(Guest guest)
    {
        if (guest.InvitationToken != null && guest.InvitationTokenExpiresAt > DateTime.UtcNow)
        {
            return;
        }

        guest.InvitationToken = GenerateToken();
        guest.InvitationTokenExpiresAt = DateTime.UtcNow.Add(InvitationTokenLifetime);
        await guestRepository.UpdateAsync(guest);
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return DefaultLanguage;
        }

        return language.Trim().ToLowerInvariant();
    }

    private static string GenerateToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
