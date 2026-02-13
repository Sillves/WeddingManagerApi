using System.Text.Json;
using Riok.Mapperly.Abstractions;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Application.Mappings;

[Mapper]
public partial class ApplicationMapper
{
    // Wedding mappings
    public partial WeddingDto WeddingToDto(Wedding wedding);
    public partial WeddingPublicDto WeddingToPublicDto(Wedding wedding);
    public partial Wedding CreateWeddingRequestToWedding(CreateWeddingRequestDto dto);
    public partial IEnumerable<WeddingDto> WeddingsToDto(IEnumerable<Wedding> weddings);

    // Guest mappings
    public partial GuestDto GuestToDto(Guest guest);
    public partial Guest CreateGuestRequestToGuest(CreateGuestRequestDto dto);
    public partial IEnumerable<GuestDto> GuestsToDto(IEnumerable<Guest> guests);

    // Null-safe version for collections
    public List<GuestDto> GuestsToListDto(ICollection<Guest>? guests)
    {
        if (guests == null) return new List<GuestDto>();
        return guests.Select(GuestToDto).ToList();
    }

    public void UpdateGuestFromDto(UpdateGuestRequestDto dto, Guest guest)
    {
        guest.Name = dto.Name;
        guest.Email = dto.Email;
        guest.RsvpStatus = dto.RsvpStatus;
        if (dto.PreferredLanguage != null)
            guest.PreferredLanguage = dto.PreferredLanguage;
    }

    // Event mappings - manual implementation to handle null Guests collection
    public EventDto EventToDto(Event evt)
    {
        return new EventDto
        {
            Id = evt.Id,
            WeddingId = evt.WeddingId,
            Name = evt.Name,
            Description = evt.Description,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            Location = evt.Location,
            GuestDtos = GuestsToListDto(evt.Guests)
        };
    }

    public partial Event CreateEventRequestToEvent(CreateEventRequestDto dto);

    public IEnumerable<EventDto> EventsToDto(IEnumerable<Event> events)
    {
        return events.Select(EventToDto);
    }

    public void UpdateEventFromDto(UpdateEventRequestDto dto, Event evt)
    {
        evt.Name = dto.Name;
        evt.StartDate = dto.StartDate;
        evt.EndDate = dto.EndDate;
        evt.Location = dto.Location;
        evt.Description = dto.Description;
    }

    // WeddingUser mapping - manual implementation for complex nested mapping
    public WeddingUserDto WeddingUserToDto(WeddingUser weddingUser)
    {
        return new WeddingUserDto
        {
            WeddingId = weddingUser.WeddingId,
            UserId = weddingUser.UserId,
            UserEmail = weddingUser.User?.Email ?? string.Empty,
            UserName = weddingUser.User != null
                ? $"{weddingUser.User.FirstName} {weddingUser.User.LastName}"
                : string.Empty,
            Role = weddingUser.Role,
            AddedAt = weddingUser.AddedAt,
            CanAccessGuests = weddingUser.CanAccessGuests,
            CanAccessEvents = weddingUser.CanAccessEvents,
            CanAccessExpenses = weddingUser.CanAccessExpenses,
            CanAccessWebsite = weddingUser.CanAccessWebsite,
            IsReadOnly = weddingUser.IsReadOnly,
        };
    }

    public IEnumerable<WeddingUserDto> WeddingUsersToDto(IEnumerable<WeddingUser> weddingUsers)
    {
        return weddingUsers.Select(WeddingUserToDto);
    }

    // Expense mappings
    public partial WeddingExpenseDto ExpenseToDto(WeddingExpense expense);
    public partial WeddingExpense CreateExpenseRequestToExpense(CreateWeddingExpenseRequestDto dto);
    public partial IEnumerable<WeddingExpenseDto> ExpensesToDto(IEnumerable<WeddingExpense> expenses);

    public void UpdateExpenseFromDto(UpdateWeddingExpenseRequestDto dto, WeddingExpense expense)
    {
        expense.Amount = dto.Amount;
        expense.Category = dto.Category;
        expense.Description = dto.Description;
        expense.Date = dto.Date;
        expense.Notes = dto.Notes;
    }

    public partial List<WeddingExpenseDto> ExpensesToListDto(IEnumerable<WeddingExpense> expenses);

    // Website mappings - manual implementation for JSON parsing
    public WeddingWebsiteDto WebsiteToDto(WeddingWebsite website)
    {
        return new WeddingWebsiteDto
        {
            Id = website.Id,
            WeddingId = website.WeddingId,
            WeddingSlug = website.Wedding?.Slug ?? string.Empty,
            Template = website.Template,
            Settings = JsonDocument.Parse(website.Settings),
            Content = JsonDocument.Parse(website.Content),
            IsPublished = website.IsPublished,
            PublishedAt = website.PublishedAt,
            MetaDescription = website.MetaDescription,
            CreatedAt = website.CreatedAt,
            UpdatedAt = website.UpdatedAt
        };
    }

    // Budget mappings
    public partial WeddingBudgetDto BudgetToDto(WeddingBudget budget);
    public partial BudgetAllocationDto AllocationToDto(BudgetAllocation allocation);
    public partial List<BudgetAllocationDto> AllocationsToListDto(ICollection<BudgetAllocation> allocations);

    // Media mappings
    [MapProperty(nameof(Media.S3Url), nameof(MediaUploadResponseDto.Url))]
    public partial MediaUploadResponseDto MediaToDto(Media media);
}
