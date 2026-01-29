using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public static class EventValidation
{
    public static Result ValidateInput(CreateEventRequestDto requestDto)
    {
        return ValidateInput(requestDto.Name, requestDto.Location, requestDto.StartDate, requestDto.EndDate);
    }

    public static Result ValidateInput(UpdateEventRequestDto requestDto)
    {
        return ValidateInput(requestDto.Name, requestDto.Location, requestDto.StartDate, requestDto.EndDate);
    }

    private static Result ValidateInput(string name, string location, DateTime startDate, DateTime endDate)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add(new Error(ErrorCodes.Validation, "Event name is required"));

        if (string.IsNullOrWhiteSpace(location))
            errors.Add(new Error(ErrorCodes.Validation, "Event location is required"));

        if (startDate == default)
            errors.Add(new Error(ErrorCodes.Validation, "Event start date is required"));

        if (endDate == default)
            errors.Add(new Error(ErrorCodes.Validation, "Event end date is required"));

        if (endDate < startDate)
            errors.Add(new Error(ErrorCodes.Validation, "Event end date must be on or after start date"));

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }
}
