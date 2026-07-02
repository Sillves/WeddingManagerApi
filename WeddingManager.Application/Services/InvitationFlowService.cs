using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class InvitationFlowService(
    IInvitationFlowRepository flowRepository,
    IRsvpResponseRepository responseRepository,
    IEventRepository eventRepository,
    ApplicationMapper mapper) : IInvitationFlowService
{
    public async Task<Result<IEnumerable<InvitationFlowDto>>> GetByWeddingIdAsync(Guid weddingId)
    {
        var flows = (await flowRepository.GetByWeddingIdAsync(weddingId)).ToList();
        var counts = await responseRepository.GetCountsByWeddingAsync(weddingId);
        var dtos = flows.Select(f => mapper.InvitationFlowToDto(f, counts.GetValueOrDefault(f.Id)));
        return Result<IEnumerable<InvitationFlowDto>>.Ok(dtos);
    }

    public async Task<Result<InvitationFlowDto>> CreateAsync(Guid weddingId, CreateInvitationFlowRequestDto requestDto)
    {
        var passcode = NormalizePasscode(requestDto.Passcode);

        var validation = InvitationFlowValidation.ValidateInput(
            requestDto.Name, passcode, requestDto.CustomQuestions);
        if (!validation.IsSuccess)
            return Result<InvitationFlowDto>.Fail(validation.Errors);

        var existing = (await flowRepository.GetByWeddingIdAsync(weddingId)).ToList();
        var exclusivity = ValidateExclusivity(existing, editingFlowId: null, passcode);
        if (!exclusivity.IsSuccess)
            return Result<InvitationFlowDto>.Fail(exclusivity.Errors);

        var eventValidation = await ValidateEventIdsAsync(weddingId, requestDto.EventIds);
        if (!eventValidation.IsSuccess)
            return Result<InvitationFlowDto>.Fail(eventValidation.Errors);

        var flow = new InvitationFlow
        {
            Id = Guid.NewGuid(),
            WeddingId = weddingId,
            Name = requestDto.Name.Trim(),
            Passcode = passcode,
            IncludePlusOne = requestDto.IncludePlusOne,
            CustomQuestions = NormalizeQuestions(requestDto.CustomQuestions),
            EventIds = requestDto.EventIds.Distinct().ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await flowRepository.AddAsync(flow);
        return Result<InvitationFlowDto>.Ok(mapper.InvitationFlowToDto(flow, 0));
    }

    public async Task<Result<InvitationFlowDto>> UpdateAsync(
        Guid weddingId, Guid flowId, UpdateInvitationFlowRequestDto requestDto)
    {
        var flow = await flowRepository.GetByIdAsync(flowId);
        if (flow == null || flow.WeddingId != weddingId)
            return Result<InvitationFlowDto>.Fail(new Error(ErrorCodes.NotFound, "Invitation flow not found"));

        var passcode = NormalizePasscode(requestDto.Passcode);

        var validation = InvitationFlowValidation.ValidateInput(
            requestDto.Name, passcode, requestDto.CustomQuestions);
        if (!validation.IsSuccess)
            return Result<InvitationFlowDto>.Fail(validation.Errors);

        var existing = (await flowRepository.GetByWeddingIdAsync(weddingId)).ToList();
        var exclusivity = ValidateExclusivity(existing, editingFlowId: flowId, passcode);
        if (!exclusivity.IsSuccess)
            return Result<InvitationFlowDto>.Fail(exclusivity.Errors);

        var eventValidation = await ValidateEventIdsAsync(weddingId, requestDto.EventIds);
        if (!eventValidation.IsSuccess)
            return Result<InvitationFlowDto>.Fail(eventValidation.Errors);

        flow.Name = requestDto.Name.Trim();
        flow.Passcode = passcode;
        flow.IncludePlusOne = requestDto.IncludePlusOne;
        flow.CustomQuestions = NormalizeQuestions(requestDto.CustomQuestions);
        flow.EventIds = requestDto.EventIds.Distinct().ToList();
        flow.UpdatedAt = DateTime.UtcNow;

        await flowRepository.UpdateAsync(flow);
        var count = await responseRepository.CountByFlowAsync(flowId);
        return Result<InvitationFlowDto>.Ok(mapper.InvitationFlowToDto(flow, count));
    }

    public async Task<Result> DeleteAsync(Guid weddingId, Guid flowId, bool force)
    {
        var flow = await flowRepository.GetByIdAsync(flowId);
        if (flow == null || flow.WeddingId != weddingId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Invitation flow not found"));

        if (!force)
        {
            var count = await responseRepository.CountByFlowAsync(flowId);
            if (count > 0)
                return Result.Fail(new Error(ErrorCodes.Conflict,
                    $"Flow has {count} response(s). Pass force=true to delete it and its responses."));
        }

        await flowRepository.DeleteAsync(flowId);
        return Result.Ok();
    }

    // Either N passcoded flows, or exactly one open (no-passcode) flow.
    private static Result ValidateExclusivity(List<InvitationFlow> existing, Guid? editingFlowId, string? passcode)
    {
        var others = existing.Where(f => f.Id != editingFlowId).ToList();

        if (passcode == null)
        {
            // An open flow must be the only flow for the wedding.
            if (others.Count > 0)
                return Result.Fail(new Error(ErrorCodes.Conflict,
                    "An open (no-passcode) flow must be the only flow. Remove the other flows or add a passcode."));
            return Result.Ok();
        }

        // Passcoded flow: no open flow may coexist, and the passcode must be unique.
        if (others.Any(f => f.Passcode == null))
            return Result.Fail(new Error(ErrorCodes.Conflict,
                "This wedding has an open flow. Remove it before adding passcode-protected flows."));

        if (others.Any(f => string.Equals(f.Passcode, passcode, StringComparison.Ordinal)))
            return Result.Fail(new Error(ErrorCodes.Conflict, "Another flow already uses this passcode."));

        return Result.Ok();
    }

    private async Task<Result> ValidateEventIdsAsync(Guid weddingId, List<Guid> eventIds)
    {
        if (eventIds.Count == 0)
            return Result.Ok();

        var weddingEventIds = (await eventRepository.GetByWeddingIdForPublicAsync(weddingId))
            .Select(e => e.Id)
            .ToHashSet();

        return eventIds.All(weddingEventIds.Contains)
            ? Result.Ok()
            : Result.Fail(new Error(ErrorCodes.Validation, "One or more selected events do not belong to this wedding."));
    }

    private static string? NormalizePasscode(string? passcode) =>
        string.IsNullOrWhiteSpace(passcode) ? null : passcode.Trim();

    private static List<QuestionDefinition> NormalizeQuestions(List<QuestionDefinition> questions)
    {
        foreach (var q in questions)
        {
            if (q.Id == Guid.Empty) q.Id = Guid.NewGuid();
            q.Label = q.Label.Trim();
        }
        return questions;
    }
}
