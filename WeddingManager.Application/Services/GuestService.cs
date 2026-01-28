
using AutoMapper;
using Microsoft.Extensions.Logging;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Services;

public class GuestService(
    IGuestRepository guestRepository,
    IWeddingRepository weddingRepository,
    IEmailService emailService,
    IMapper mapper,
    ILogger<GuestService> logger)
    : IGuestService
{
    private const string DefaultLanguage = "en";
    private static readonly TimeSpan InvitationTokenLifetime = TimeSpan.FromDays(30);

    public async Task<GuestDto?> GetByIdAsync(Guid guestId)
    {
        var guest = await guestRepository.GetByIdAsync(guestId);
        return guest != null ? mapper.Map<GuestDto>(guest) : null;
    }

    public async Task<IEnumerable<GuestDto>> GetByWeddingIdAsync(Guid weddingId)
    {
        var guests = await guestRepository.GetByWeddingIdAsync(weddingId);
        return mapper.Map<IEnumerable<GuestDto>>(guests);
    }

    public async Task<GuestDto> CreateGuestAsync(Guid weddingId, CreateGuestRequestDto requestDto)
    {
        GuestValidation.ValidateInput(requestDto);

        var existingGuest = await guestRepository.GetByEmailAsync(weddingId, requestDto.Email);
        if (existingGuest != null)
            throw new InvalidOperationException($"A guest with email {requestDto.Email} already exists for this wedding");

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
        return mapper.Map<GuestDto>(guest);
    }

    public async Task<GuestDto> UpdateGuestAsync(Guid guestId, UpdateGuestRequestDto requestDto)
    {
        var guest = await guestRepository.GetByIdAsync(guestId)
            ?? throw new ArgumentException($"Guest with id {guestId} not found");

        GuestValidation.ValidateInput(requestDto);

        if (guest.Email != requestDto.Email)
        {
            var existingGuest = await guestRepository.GetByEmailAsync(guest.WeddingId, requestDto.Email);
            if (existingGuest != null)
                throw new InvalidOperationException($"A guest with email {requestDto.Email} already exists for this wedding");
        }

        guest.Name = requestDto.Name;
        guest.Email = requestDto.Email;
        guest.RsvpStatus = requestDto.RsvpStatus;
        guest.PreferredLanguage = NormalizeLanguage(requestDto.PreferredLanguage);

        await guestRepository.UpdateAsync(guest);
        return mapper.Map<GuestDto>(guest);
    }

    public async Task DeleteGuestAsync(Guid guestId)
    {
        var guest = await guestRepository.GetByIdAsync(guestId)
            ?? throw new ArgumentException($"Guest with id {guestId} not found");

        await guestRepository.DeleteAsync(guestId);
    }

    public async Task<GuestDto?> SubmitRsvpAsync(Guid weddingId, RsvpSubmitRequestDto requestDto)
    {
        GuestValidation.ValidateInput(requestDto);

        var guest = await guestRepository.GetByEmailAsync(weddingId, requestDto.Email);
        if (guest == null)
            return null;

        guest.Name = requestDto.Name;
        guest.RsvpStatus = requestDto.RsvpStatus;

        await guestRepository.UpdateAsync(guest);

        // var wedding = await weddingRepository.GetByIdAsync(weddingId);
        // if (wedding != null)
        // {
        //     await emailService.SendRsvpConfirmationAsync(guest, wedding);
        // }

        return mapper.Map<GuestDto>(guest);
    }

    public async Task SendInvitationAsync(Guid weddingId, Guid guestId)
    {
        var wedding = await weddingRepository.GetByIdAsync(weddingId)
            ?? throw new KeyNotFoundException("Wedding not found");

        var guest = await guestRepository.GetByIdAsync(guestId)
            ?? throw new KeyNotFoundException("Guest not found");

        if (guest.WeddingId != weddingId)
        {
            throw new ArgumentException("Guest does not belong to this wedding");
        }

        await EnsureInvitationTokenAsync(guest);

        try
        {
            await emailService.SendInvitationAsync(guest, wedding);
            guest.InvitationSentAt = DateTime.UtcNow;
            await guestRepository.UpdateAsync(guest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invitation to guest {GuestId} for wedding {WeddingId}", guestId, weddingId);
            throw new InvalidOperationException("Failed to send invitation email");
        }
    }

    public async Task<InvitationSendResultDto> SendInvitationsAsync(Guid weddingId, IReadOnlyCollection<Guid>? guestIds)
    {
        var wedding = await weddingRepository.GetByIdAsync(weddingId)
            ?? throw new KeyNotFoundException("Wedding not found");

        List<Guest> guests;
        if (guestIds is { Count: > 0 })
        {
            guests = (await guestRepository.GetByIdsAsync(weddingId, guestIds)).ToList();
            var missingIds = guestIds.Except(guests.Select(g => g.Id)).ToList();
            if (missingIds.Count > 0)
            {
                throw new ArgumentException($"Guests not found: {string.Join(", ", missingIds)}");
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
            return new InvitationSendResultDto();
        }

        var result = new InvitationSendResultDto();

        foreach (var guest in guests)
        {
            try
            {
                await EnsureInvitationTokenAsync(guest);
                await emailService.SendInvitationAsync(guest, wedding);
                guest.InvitationSentAt = DateTime.UtcNow;
                await guestRepository.UpdateAsync(guest);
                result.SentCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invitation to guest {GuestId} for wedding {WeddingId}", guest.Id, weddingId);
                result.FailedCount++;
                result.FailedGuestIds.Add(guest.Id);
            }
        }

        return result;
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
