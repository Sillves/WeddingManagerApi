
using WeddingManager.Domain.DTO;

namespace WeddingManager.Application.Services;

public static class GuestValidation
{
    public static void ValidateInput(CreateGuestRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Name))
            throw new ArgumentException("Guest name is required");

        if (string.IsNullOrWhiteSpace(requestDto.Email))
            throw new ArgumentException("Guest email is required");

        if (!IsValidEmail(requestDto.Email))
            throw new ArgumentException("Guest email is not valid");

        if (!IsSupportedLanguage(requestDto.PreferredLanguage))
            throw new ArgumentException("Guest language is not supported");
    }

    public static void ValidateInput(UpdateGuestRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Name))
            throw new ArgumentException("Guest name is required");

        if (string.IsNullOrWhiteSpace(requestDto.Email))
            throw new ArgumentException("Guest email is required");

        if (!IsValidEmail(requestDto.Email))
            throw new ArgumentException("Guest email is not valid");

        if (!IsSupportedLanguage(requestDto.PreferredLanguage))
            throw new ArgumentException("Guest language is not supported");
    }

    public static void ValidateInput(RsvpSubmitRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Name))
            throw new ArgumentException("Guest name is required");

        if (string.IsNullOrWhiteSpace(requestDto.Email))
            throw new ArgumentException("Guest email is required");

        if (!IsValidEmail(requestDto.Email))
            throw new ArgumentException("Guest email is not valid");
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
