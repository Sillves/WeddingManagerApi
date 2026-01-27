using WeddingManager.Domain.DTO;

namespace WeddingManager.Application.Services;

public static class EventValidation
{
    public static void ValidateInput(CreateEventRequestDto requestDto)
    {
        ValidateInput(requestDto.Name, requestDto.Location, requestDto.StartDate, requestDto.EndDate);
    }

    public static void ValidateInput(UpdateEventRequestDto requestDto)
    {
        ValidateInput(requestDto.Name, requestDto.Location, requestDto.StartDate, requestDto.EndDate);
    }

    private static void ValidateInput(string name, string location, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Event name is required");

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Event location is required");

        if (startDate == default)
            throw new ArgumentException("Event start date is required");

        if (endDate == default)
            throw new ArgumentException("Event end date is required");

        if (endDate < startDate)
            throw new ArgumentException("Event end date must be on or after start date");
    }
}
