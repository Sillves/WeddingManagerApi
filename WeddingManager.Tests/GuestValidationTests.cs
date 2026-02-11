using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Tests;

public class GuestValidationTests
{
    [Fact]
    public void ValidateInput_Create_ReturnsErrorsForInvalidFields()
    {
        var request = new CreateGuestRequestDto
        {
            Name = "",
            Email = "invalid-email",
            PreferredLanguage = "es"
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Guest name is required");
        Assert.Contains(result.Errors, e => e.Message == "Guest email is not valid");
        Assert.Contains(result.Errors, e => e.Message == "Guest language is not supported");
    }

    [Fact]
    public void ValidateInput_Create_ReturnsOkForValidInput()
    {
        var request = new CreateGuestRequestDto
        {
            Name = "Jamie",
            Email = "jamie@example.com",
            RsvpStatus = RsvpStatus.Pending,
            PreferredLanguage = "nl"
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateInput_Update_ReturnsOkForValidFields()
    {
        var request = new UpdateGuestRequestDto
        {
            Name = "Guest",
            Email = "guest@example.com",
            PreferredLanguage = "en"
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorForMissingName()
    {
        var request = new UpdateGuestRequestDto
        {
            Name = "",
            Email = "guest@example.com",
            PreferredLanguage = "en"
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Guest name is required");
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorForInvalidEmail()
    {
        var request = new UpdateGuestRequestDto
        {
            Name = "Guest",
            Email = "not-an-email",
            PreferredLanguage = "en"
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Guest email is not valid");
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorForUnsupportedLanguage()
    {
        var request = new UpdateGuestRequestDto
        {
            Name = "Guest",
            Email = "guest@example.com",
            PreferredLanguage = "de"
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Guest language is not supported");
    }

    [Fact]
    public void ValidateInput_Rsvp_ReturnsOkForValidInput()
    {
        var request = new RsvpSubmitRequestDto
        {
            Name = "Guest",
            Email = "guest@example.com",
            RsvpStatus = RsvpStatus.Accepted
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateInput_Rsvp_ReturnsErrorForMissingName()
    {
        var request = new RsvpSubmitRequestDto
        {
            Name = "",
            Email = "guest@example.com"
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Guest name is required");
    }

    [Fact]
    public void ValidateInput_Rsvp_ReturnsErrorsForMissingEmail()
    {
        var request = new RsvpSubmitRequestDto
        {
            Name = "Guest",
            Email = ""
        };

        var result = GuestValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Guest email is required");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("first+last@company.co.uk")]
    public void IsValidEmail_ReturnsTrueForValidFormats(string email)
    {
        Assert.True(GuestValidation.IsValidEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-domain@")]
    public void IsValidEmail_ReturnsFalseForInvalidFormats(string email)
    {
        Assert.False(GuestValidation.IsValidEmail(email));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("nl")]
    [InlineData("fr")]
    public void IsSupportedLanguage_ReturnsTrueForSupported(string language)
    {
        Assert.True(GuestValidation.IsSupportedLanguage(language));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void IsSupportedLanguage_ReturnsTrueForNullOrEmpty(string? language)
    {
        Assert.True(GuestValidation.IsSupportedLanguage(language));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("jp")]
    public void IsSupportedLanguage_ReturnsFalseForUnsupported(string language)
    {
        Assert.False(GuestValidation.IsSupportedLanguage(language));
    }
}
