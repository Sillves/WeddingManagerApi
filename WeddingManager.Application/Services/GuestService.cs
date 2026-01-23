
using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Services;

public class GuestService(
    IGuestRepository guestRepository,
    IWeddingRepository weddingRepository,
    IEmailService emailService,
    IMapper mapper)
    : IGuestService
{
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

        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding != null)
        {
            await emailService.SendRsvpConfirmationAsync(guest, wedding);
        }

        return mapper.Map<GuestDto>(guest);
    }
}
