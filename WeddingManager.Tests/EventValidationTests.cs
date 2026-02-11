using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;

namespace WeddingManager.Tests;

public class EventValidationTests
{
    [Fact]
    public void ValidateInput_Create_ReturnsErrorsForMissingFields()
    {
        var request = new CreateEventRequestDto
        {
            Name = "",
            Location = "",
            StartDate = default,
            EndDate = default
        };

        var result = EventValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Event name is required");
        Assert.Contains(result.Errors, e => e.Message == "Event location is required");
        Assert.Contains(result.Errors, e => e.Message == "Event start date is required");
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorWhenEndBeforeStart()
    {
        var request = new UpdateEventRequestDto
        {
            Name = "Reception",
            Location = "Ghent",
            StartDate = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = EventValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Event end date must be on or after start date");
    }

    [Fact]
    public void ValidateInput_ReturnsOkForValidData()
    {
        var request = new CreateEventRequestDto
        {
            Name = "Ceremony",
            Location = "Ghent",
            StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc)
        };

        var result = EventValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }
}
