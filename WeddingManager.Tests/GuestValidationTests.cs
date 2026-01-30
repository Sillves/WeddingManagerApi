using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;

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
}
