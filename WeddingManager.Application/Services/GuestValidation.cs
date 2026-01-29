
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public static class GuestValidation
{
    public static Result ValidateInput(CreateGuestRequestDto requestDto)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(requestDto.Name))
            errors.Add(new Error(ErrorCodes.Validation, "Guest name is required"));

        if (string.IsNullOrWhiteSpace(requestDto.Email))
            errors.Add(new Error(ErrorCodes.Validation, "Guest email is required"));

        if (!IsValidEmail(requestDto.Email))
            errors.Add(new Error(ErrorCodes.Validation, "Guest email is not valid"));

        if (!IsSupportedLanguage(requestDto.PreferredLanguage))
            errors.Add(new Error(ErrorCodes.Validation, "Guest language is not supported"));

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    public static Result ValidateInput(UpdateGuestRequestDto requestDto)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(requestDto.Name))
            errors.Add(new Error(ErrorCodes.Validation, "Guest name is required"));

        if (string.IsNullOrWhiteSpace(requestDto.Email))
            errors.Add(new Error(ErrorCodes.Validation, "Guest email is required"));

        if (!IsValidEmail(requestDto.Email))
            errors.Add(new Error(ErrorCodes.Validation, "Guest email is not valid"));

        if (!IsSupportedLanguage(requestDto.PreferredLanguage))
            errors.Add(new Error(ErrorCodes.Validation, "Guest language is not supported"));

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    public static Result ValidateInput(RsvpSubmitRequestDto requestDto)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(requestDto.Name))
            errors.Add(new Error(ErrorCodes.Validation, "Guest name is required"));

        if (string.IsNullOrWhiteSpace(requestDto.Email))
            errors.Add(new Error(ErrorCodes.Validation, "Guest email is required"));

        if (!IsValidEmail(requestDto.Email))
            errors.Add(new Error(ErrorCodes.Validation, "Guest email is not valid"));

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSupportedLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return true;
        }

        return language.Equals("en", StringComparison.OrdinalIgnoreCase)
               || language.Equals("nl", StringComparison.OrdinalIgnoreCase)
               || language.Equals("fr", StringComparison.OrdinalIgnoreCase);
    }
}
