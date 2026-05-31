using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public static class InvitationFlowValidation
{
    public static Result ValidateInput(
        string name,
        string? passcode,
        List<QuestionDefinition> questions,
        List<CustomEventDefinition> customEvents)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(new Error(ErrorCodes.Validation, "Flow name is required"));
        else if (name.Length > 100)
            errors.Add(new Error(ErrorCodes.Validation, "Flow name must be 100 characters or fewer"));

        if (passcode != null)
        {
            if (string.IsNullOrWhiteSpace(passcode))
                errors.Add(new Error(ErrorCodes.Validation, "Passcode cannot be blank; omit it for an open flow"));
            else if (passcode.Length > 50)
                errors.Add(new Error(ErrorCodes.Validation, "Passcode must be 50 characters or fewer"));
        }

        foreach (var q in questions)
        {
            if (string.IsNullOrWhiteSpace(q.Label))
            {
                errors.Add(new Error(ErrorCodes.Validation, "Every custom question needs a label"));
                continue;
            }

            var needsOptions = q.Type is RsvpQuestionType.SingleChoice or RsvpQuestionType.MultiChoice;
            var hasOptions = q.Options is { Count: > 0 };
            if (needsOptions && !hasOptions)
                errors.Add(new Error(ErrorCodes.Validation, $"Question \"{q.Label}\" needs at least one option"));
            if (!needsOptions && hasOptions)
                errors.Add(new Error(ErrorCodes.Validation, $"Question \"{q.Label}\" cannot have options for its type"));
            if (needsOptions && q.Options!.Any(string.IsNullOrWhiteSpace))
                errors.Add(new Error(ErrorCodes.Validation, $"Question \"{q.Label}\" has a blank option"));
        }

        foreach (var ce in customEvents)
        {
            if (string.IsNullOrWhiteSpace(ce.Name))
                errors.Add(new Error(ErrorCodes.Validation, "Every custom event needs a name"));
        }

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }
}
