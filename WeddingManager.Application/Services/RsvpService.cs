using System.Text.Json;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class RsvpService(
    IWeddingRepository weddingRepository,
    IInvitationFlowRepository flowRepository,
    IRsvpResponseRepository responseRepository,
    IGuestRepository guestRepository,
    IEventRepository eventRepository) : IRsvpService
{
    public async Task<Result<RsvpFlowStateDto>> GetFlowStateAsync(string weddingSlugOrId)
    {
        var wedding = await weddingRepository.GetByIdOrSlugAsync(weddingSlugOrId);
        if (wedding == null)
            return Result<RsvpFlowStateDto>.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"));

        var flows = (await flowRepository.GetByWeddingIdAsync(wedding.Id)).ToList();
        if (flows.Count == 0)
            return Result<RsvpFlowStateDto>.Ok(new RsvpFlowStateDto { HasFlows = false });

        var openFlow = flows.FirstOrDefault(f => f.Passcode == null);
        if (openFlow != null)
        {
            var dto = await BuildPublicFlowAsync(wedding.Id, openFlow);
            return Result<RsvpFlowStateDto>.Ok(new RsvpFlowStateDto
            {
                HasFlows = true,
                RequiresPasscode = false,
                Flow = dto
            });
        }

        return Result<RsvpFlowStateDto>.Ok(new RsvpFlowStateDto
        {
            HasFlows = true,
            RequiresPasscode = true
        });
    }

    public async Task<Result<RsvpFlowPublicDto>> UnlockAsync(string weddingSlugOrId, string passcode)
    {
        var wedding = await weddingRepository.GetByIdOrSlugAsync(weddingSlugOrId);
        if (wedding == null)
            return Result<RsvpFlowPublicDto>.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"));

        if (string.IsNullOrWhiteSpace(passcode))
            return Result<RsvpFlowPublicDto>.Fail(new Error(ErrorCodes.Unauthorized, "Invalid passcode"));

        var flow = await flowRepository.GetByPasscodeAsync(wedding.Id, passcode.Trim());
        if (flow == null)
            return Result<RsvpFlowPublicDto>.Fail(new Error(ErrorCodes.Unauthorized, "Invalid passcode"));

        var dto = await BuildPublicFlowAsync(wedding.Id, flow);
        return Result<RsvpFlowPublicDto>.Ok(dto);
    }

    public async Task<Result<RsvpSubmitResultDto>> SubmitAsync(
        string weddingSlugOrId, Guid flowId, RsvpFlowSubmitRequestDto requestDto)
    {
        var wedding = await weddingRepository.GetByIdOrSlugAsync(weddingSlugOrId);
        if (wedding == null)
            return Result<RsvpSubmitResultDto>.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"));

        var flow = await flowRepository.GetByIdAsync(flowId);
        if (flow == null || flow.WeddingId != wedding.Id)
            return Result<RsvpSubmitResultDto>.Fail(new Error(ErrorCodes.NotFound, "Invitation flow not found"));

        var validEventIds = await GetValidEventIdsAsync(wedding.Id, flow);
        var validation = RsvpSubmissionValidation.Validate(flow, requestDto, validEventIds);
        if (!validation.IsSuccess)
            return Result<RsvpSubmitResultDto>.Fail(validation.Errors);

        var name = requestDto.Name.Trim();
        var surname = requestDto.Surname.Trim();
        var email = requestDto.Email.Trim();

        var duplicate = await guestRepository.FindByDedupeKeyAsync(wedding.Id, email, name, surname);
        if (duplicate != null)
            return Result<RsvpSubmitResultDto>.Fail(new Error(ErrorCodes.Conflict,
                "An RSVP with this name and email already exists for this wedding."));

        var attendingEventIds = requestDto.Status == RsvpResponseStatus.Attending
            ? requestDto.AttendingEventIds.Where(validEventIds.Contains).Distinct().ToList()
            : new List<Guid>();

        var guestStatus = requestDto.Status == RsvpResponseStatus.Attending ? RsvpStatus.Accepted : RsvpStatus.Declined;

        var mainGuest = new Guest
        {
            Id = Guid.NewGuid(),
            WeddingId = wedding.Id,
            Name = name,
            Surname = surname,
            Email = email,
            Dietary = string.IsNullOrWhiteSpace(requestDto.Dietary) ? null : requestDto.Dietary.Trim(),
            RsvpStatus = guestStatus
        };

        var guests = new List<Guest> { mainGuest };
        var responses = new List<RsvpResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = wedding.Id,
                InvitationFlowId = flow.Id,
                GuestId = mainGuest.Id,
                Status = requestDto.Status,
                AttendingEventIds = attendingEventIds,
                CustomAnswers = JsonSerializer.Serialize(requestDto.CustomAnswers, RsvpJson.Options),
                SubmittedAt = DateTime.UtcNow
            }
        };

        Guid? plusOneGuestId = null;
        if (requestDto is { PlusOneAttending: true } && flow.IncludePlusOne && !string.IsNullOrWhiteSpace(requestDto.PlusOneName))
        {
            var plusOne = new Guest
            {
                Id = Guid.NewGuid(),
                WeddingId = wedding.Id,
                Name = requestDto.PlusOneName.Trim(),
                Surname = null,
                Email = email,
                Dietary = string.IsNullOrWhiteSpace(requestDto.PlusOneDietary) ? null : requestDto.PlusOneDietary.Trim(),
                RsvpStatus = RsvpStatus.Accepted,
                PlusOneOfGuestId = mainGuest.Id
            };
            plusOneGuestId = plusOne.Id;
            guests.Add(plusOne);
            responses.Add(new RsvpResponse
            {
                Id = Guid.NewGuid(),
                WeddingId = wedding.Id,
                InvitationFlowId = flow.Id,
                GuestId = plusOne.Id,
                Status = RsvpResponseStatus.Attending,
                AttendingEventIds = attendingEventIds,
                CustomAnswers = "{}",
                SubmittedAt = DateTime.UtcNow
            });
        }

        var saved = await responseRepository.SubmitAsync(guests, responses);
        if (!saved)
            return Result<RsvpSubmitResultDto>.Fail(new Error(ErrorCodes.Conflict,
                "An RSVP with this name and email already exists for this wedding."));

        return Result<RsvpSubmitResultDto>.Ok(new RsvpSubmitResultDto
        {
            ResponseId = responses[0].Id,
            GuestId = mainGuest.Id,
            PlusOneGuestId = plusOneGuestId
        });
    }

    public async Task<Result<IEnumerable<RsvpResponseDto>>> GetResponsesAsync(Guid weddingId, Guid? flowId)
    {
        var responses = (await responseRepository.GetByWeddingIdAsync(weddingId, flowId)).ToList();
        var events = (await eventRepository.GetByWeddingIdForPublicAsync(weddingId)).ToList();
        var flows = (await flowRepository.GetByWeddingIdAsync(weddingId)).ToList();

        var eventNames = events.ToDictionary(e => e.Id, e => e.Name);
        var customEventNames = flows
            .SelectMany(f => f.CustomEvents)
            .GroupBy(ce => ce.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var dtos = responses.Select(r =>
        {
            var flow = flows.FirstOrDefault(f => f.Id == r.InvitationFlowId);
            return new RsvpResponseDto
            {
                Id = r.Id,
                InvitationFlowId = r.InvitationFlowId,
                FlowName = flow?.Name ?? string.Empty,
                GuestId = r.GuestId,
                Name = r.Guest.Name,
                Surname = r.Guest.Surname,
                Email = r.Guest.Email,
                Dietary = r.Guest.Dietary,
                Status = r.Status,
                AttendingEventIds = r.AttendingEventIds,
                AttendingEventNames = r.AttendingEventIds
                    .Select(id => eventNames.GetValueOrDefault(id)
                        ?? customEventNames.GetValueOrDefault(id)
                        ?? "(deleted event)")
                    .ToList(),
                CustomAnswers = DeserializeAnswers(r.CustomAnswers),
                IsPlusOne = r.Guest.PlusOneOfGuestId != null,
                PlusOneOfGuestId = r.Guest.PlusOneOfGuestId,
                SubmittedAt = r.SubmittedAt
            };
        });

        return Result<IEnumerable<RsvpResponseDto>>.Ok(dtos);
    }

    private async Task<HashSet<Guid>> GetValidEventIdsAsync(Guid weddingId, InvitationFlow flow)
    {
        var realEventIds = (await eventRepository.GetByWeddingIdForPublicAsync(weddingId))
            .Select(e => e.Id)
            .ToHashSet();
        var valid = flow.EventIds.Where(realEventIds.Contains).ToHashSet();
        foreach (var ce in flow.CustomEvents)
            valid.Add(ce.Id);
        return valid;
    }

    private async Task<RsvpFlowPublicDto> BuildPublicFlowAsync(Guid weddingId, InvitationFlow flow)
    {
        var realEvents = (await eventRepository.GetByWeddingIdForPublicAsync(weddingId))
            .Where(e => flow.EventIds.Contains(e.Id))
            .Select(e => new RsvpEventOptionDto
            {
                Id = e.Id,
                Name = e.Name,
                StartDate = e.StartDate,
                Location = e.Location
            });

        var customEvents = flow.CustomEvents.Select(ce => new RsvpEventOptionDto
        {
            Id = ce.Id,
            Name = ce.Name,
            StartDate = ce.StartDate,
            Location = ce.Location
        });

        var allEvents = realEvents
            .Concat(customEvents)
            .OrderBy(e => e.StartDate ?? DateTime.MaxValue)
            .ToList();

        return new RsvpFlowPublicDto
        {
            FlowId = flow.Id,
            WeddingId = weddingId,
            IncludePlusOne = flow.IncludePlusOne,
            Questions = flow.CustomQuestions,
            Events = allEvents
        };
    }

    private static Dictionary<string, JsonElement> DeserializeAnswers(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return new Dictionary<string, JsonElement>();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, RsvpJson.Options)
            ?? new Dictionary<string, JsonElement>();
    }
}
