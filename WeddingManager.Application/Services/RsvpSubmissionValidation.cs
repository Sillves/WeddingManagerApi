using System.Text.Json;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public static class RsvpSubmissionValidation
{
    public static Result Validate(InvitationFlow flow, RsvpFlowSubmitRequestDto dto, ISet<Guid> validEventIds)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add(new Error(ErrorCodes.Validation, "Name is required"));
        if (string.IsNullOrWhiteSpace(dto.Surname))
            errors.Add(new Error(ErrorCodes.Validation, "Surname is required"));
        if (string.IsNullOrWhiteSpace(dto.Email) || !GuestValidation.IsValidEmail(dto.Email))
            errors.Add(new Error(ErrorCodes.Validation, "A valid email is required"));

        if (dto.Status == RsvpResponseStatus.Attending)
        {
            foreach (var id in dto.AttendingEventIds)
            {
                if (!validEventIds.Contains(id))
                    errors.Add(new Error(ErrorCodes.Validation, "An selected event is not part of this invitation"));
            }
        }

        if (dto.PlusOneAttending)
        {
            if (!flow.IncludePlusOne)
                errors.Add(new Error(ErrorCodes.Validation, "This invitation does not allow a plus-one"));
            else if (string.IsNullOrWhiteSpace(dto.PlusOneName))
                errors.Add(new Error(ErrorCodes.Validation, "Plus-one name is required when bringing a plus-one"));
        }

        ValidateCustomAnswers(flow, dto, errors);

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    private static void ValidateCustomAnswers(InvitationFlow flow, RsvpFlowSubmitRequestDto dto, List<Error> errors)
    {
        foreach (var question in flow.CustomQuestions)
        {
            var key = question.Id.ToString();
            var present = dto.CustomAnswers.TryGetValue(key, out var answer) && !IsEmpty(answer);

            if (!present)
            {
                if (question.Required)
                    errors.Add(new Error(ErrorCodes.Validation, $"\"{question.Label}\" is required"));
                continue;
            }

            switch (question.Type)
            {
                case RsvpQuestionType.YesNo:
                    if (answer.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                        errors.Add(new Error(ErrorCodes.Validation, $"\"{question.Label}\" must be yes or no"));
                    break;

                case RsvpQuestionType.FreeText:
                    if (answer.ValueKind != JsonValueKind.String)
                        errors.Add(new Error(ErrorCodes.Validation, $"\"{question.Label}\" must be text"));
                    break;

                case RsvpQuestionType.SingleChoice:
                    if (answer.ValueKind != JsonValueKind.String ||
                        !(question.Options?.Contains(answer.GetString()!) ?? false))
                        errors.Add(new Error(ErrorCodes.Validation, $"\"{question.Label}\" has an invalid choice"));
                    break;

                case RsvpQuestionType.MultiChoice:
                    if (answer.ValueKind != JsonValueKind.Array ||
                        answer.EnumerateArray().Any(v =>
                            v.ValueKind != JsonValueKind.String ||
                            !(question.Options?.Contains(v.GetString()!) ?? false)))
                        errors.Add(new Error(ErrorCodes.Validation, $"\"{question.Label}\" has an invalid selection"));
                    break;
            }
        }
    }

    private static bool IsEmpty(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.Undefined => true,
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
            JsonValueKind.Array => value.GetArrayLength() == 0,
            _ => false
        };
}
