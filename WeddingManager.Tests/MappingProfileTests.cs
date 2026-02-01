using WeddingManager.Application.Mappings;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Tests;

public class MappingProfileTests
{
    private readonly ApplicationMapper _mapper = new();

    [Fact]
    public void EventToEventDto_MapsGuests()
    {
        var weddingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var guests = new List<Guest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Ava",
                Email = "ava@example.com",
                RsvpStatus = RsvpStatus.Accepted,
                PreferredLanguage = "en",
                InvitationSentAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                WeddingId = weddingId
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Noah",
                Email = "noah@example.com",
                RsvpStatus = RsvpStatus.Pending,
                PreferredLanguage = "nl",
                WeddingId = weddingId
            }
        };
        var source = new Event
        {
            Id = eventId,
            WeddingId = weddingId,
            Name = "Ceremony",
            StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
            Location = "Ghent",
            Guests = guests
        };

        var result = _mapper.EventToDto(source);

        Assert.NotNull(result.GuestDtos);
        Assert.Equal(2, result.GuestDtos.Count);
        Assert.Equal(guests[0].Id, result.GuestDtos[0].Id);
        Assert.Equal(guests[0].Name, result.GuestDtos[0].Name);
        Assert.Equal(guests[0].Email, result.GuestDtos[0].Email);
        Assert.Equal(guests[0].RsvpStatus, result.GuestDtos[0].RsvpStatus);
        Assert.Equal(guests[0].PreferredLanguage, result.GuestDtos[0].PreferredLanguage);
        Assert.Equal(guests[0].InvitationSentAt, result.GuestDtos[0].InvitationSentAt);
        Assert.Equal(guests[0].WeddingId, result.GuestDtos[0].WeddingId);
    }
}
