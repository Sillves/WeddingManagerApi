using Microsoft.EntityFrameworkCore;
using Moq;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Infrastructure.Data;
using WeddingManager.Infrastructure.Repositories;

namespace WeddingManager.Tests;

public class EventRepositoryTests
{
    [Fact]
    public async Task AddGuestsToEventAsync_AddsGuestsAndReturnsDetails()
    {
        var (context, repository, userId) = CreateRepository();
        var (eventEntity, guestA, guestB, _) = SeedWeddingWithEventAndGuests(context, userId);

        var result = await repository.AddGuestsToEventAsync(eventEntity.Id, [guestA.Id, guestB.Id]);

        Assert.Equal(EventGuestChangeResult.Added, result.Status);
        Assert.Equal(2, result.AddedGuestIds.Count);
        Assert.Contains(guestA.Id, result.AddedGuestIds);
        Assert.Contains(guestB.Id, result.AddedGuestIds);
        Assert.Empty(result.AlreadyInEventGuestIds);
        Assert.Empty(result.NotFoundGuestIds);

        var updatedEvent = await context.Events.Include(e => e.Guests)
            .SingleAsync(e => e.Id == eventEntity.Id);
        Assert.Equal(2, updatedEvent.Guests.Count);
    }

    [Fact]
    public async Task AddGuestsToEventAsync_ReturnsUnauthorized_WhenUserNotOwner()
    {
        var (context, repository, _) = CreateRepository();
        var (eventEntity, guestA, _, _) = SeedWeddingWithEventAndGuests(context, Guid.NewGuid());

        var result = await repository.AddGuestsToEventAsync(eventEntity.Id, [guestA.Id]);

        Assert.Equal(EventGuestChangeResult.Unauthorized, result.Status);
        Assert.Empty(result.AddedGuestIds);
    }

    [Fact]
    public async Task AddGuestsToEventAsync_ReturnsNotFound_WhenEventMissing()
    {
        var (context, repository, userId) = CreateRepository();
        var missingEventId = Guid.NewGuid();
        var guestId = SeedGuestOnly(context, userId);

        var result = await repository.AddGuestsToEventAsync(missingEventId, [guestId]);

        Assert.Equal(EventGuestChangeResult.NotFound, result.Status);
    }

    [Fact]
    public async Task AddGuestsToEventAsync_ReportsAlreadyInEventAndNotFound()
    {
        var (context, repository, userId) = CreateRepository();
        var (eventEntity, guestA, guestB, _) = SeedWeddingWithEventAndGuests(context, userId);
        eventEntity.Guests.Add(guestA);
        await context.SaveChangesAsync();

        var missingGuestId = Guid.NewGuid();
        var result = await repository.AddGuestsToEventAsync(eventEntity.Id, [guestA.Id, guestB.Id, missingGuestId]);

        Assert.Equal(EventGuestChangeResult.Added, result.Status);
        Assert.Single(result.AddedGuestIds);
        Assert.Contains(guestB.Id, result.AddedGuestIds);
        Assert.Single(result.AlreadyInEventGuestIds);
        Assert.Contains(guestA.Id, result.AlreadyInEventGuestIds);
        Assert.Single(result.NotFoundGuestIds);
        Assert.Contains(missingGuestId, result.NotFoundGuestIds);

        var updatedEvent = await context.Events.Include(e => e.Guests)
            .SingleAsync(e => e.Id == eventEntity.Id);
        Assert.Equal(2, updatedEvent.Guests.Count);
    }

    [Fact]
    public async Task RemoveGuestsFromEventAsync_RemovesGuestsAndReportsNotInEvent()
    {
        var (context, repository, userId) = CreateRepository();
        var (eventEntity, guestA, guestB, _) = SeedWeddingWithEventAndGuests(context, userId);
        eventEntity.Guests.Add(guestA);
        eventEntity.Guests.Add(guestB);
        await context.SaveChangesAsync();

        var missingGuestId = Guid.NewGuid();
        var result = await repository.RemoveGuestsFromEventAsync(eventEntity.Id, [guestA.Id, missingGuestId]);

        Assert.Equal(EventGuestChangeResult.Removed, result.Status);
        Assert.Single(result.RemovedGuestIds);
        Assert.Contains(guestA.Id, result.RemovedGuestIds);
        Assert.Single(result.NotInEventGuestIds);
        Assert.Contains(missingGuestId, result.NotInEventGuestIds);

        var updatedEvent = await context.Events.Include(e => e.Guests)
            .SingleAsync(e => e.Id == eventEntity.Id);
        Assert.Single(updatedEvent.Guests);
        Assert.Contains(guestB.Id, updatedEvent.Guests.Select(g => g.Id));
    }

    [Fact]
    public async Task RemoveGuestsFromEventAsync_ReturnsUnauthorized_WhenUserNotOwner()
    {
        var (context, repository, _) = CreateRepository();
        var (eventEntity, guestA, _, _) = SeedWeddingWithEventAndGuests(context, Guid.NewGuid());

        var result = await repository.RemoveGuestsFromEventAsync(eventEntity.Id, [guestA.Id]);

        Assert.Equal(EventGuestChangeResult.Unauthorized, result.Status);
        Assert.Empty(result.RemovedGuestIds);
    }

    [Fact]
    public async Task RemoveGuestsFromEventAsync_ReturnsNotFound_WhenEventMissing()
    {
        var (context, repository, userId) = CreateRepository();
        var missingEventId = Guid.NewGuid();
        var guestId = SeedGuestOnly(context, userId);

        var result = await repository.RemoveGuestsFromEventAsync(missingEventId, [guestId]);

        Assert.Equal(EventGuestChangeResult.NotFound, result.Status);
    }

    private static (WeddingDbContext Context, EventRepository Repository, Guid UserId) CreateRepository()
    {
        var dbName = $"EventRepo-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<WeddingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new WeddingDbContext(options);
        var userId = Guid.NewGuid();
        var userContextMock = new Mock<IUserContextService>();
        userContextMock.Setup(s => s.GetUserId()).Returns(userId);
        userContextMock.SetupGet(s => s.IsAuthenticated).Returns(true);
        userContextMock.Setup(s => s.GetUserEmail()).Returns("user@example.com");

        return (context, new EventRepository(context, userContextMock.Object), userId);
    }

    private static (Event EventEntity, Guest GuestA, Guest GuestB, Wedding Wedding) SeedWeddingWithEventAndGuests(
        WeddingDbContext context,
        Guid userId)
    {
        var user = new User
        {
            Id = userId,
            UserName = "user@example.com",
            Email = "user@example.com"
        };
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId,
            UserId = userId,
            User = user,
            Title = "Test Wedding",
            Slug = $"test-{weddingId}",
            Location = "Ghent",
            Date = DateTime.UtcNow,
            WeddingUsers = new List<WeddingUser>(),
            Guests = new List<Guest>(),
            Pages = new List<Page>(),
            Media = new List<Media>(),
            Events = new List<Event>()
        };

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Ceremony",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(1),
            Location = "Ghent",
            WeddingId = weddingId,
            Wedding = wedding,
            Guests = new List<Guest>()
        };

        var guestA = new Guest
        {
            Id = Guid.NewGuid(),
            Name = "Guest A",
            Email = "a@example.com",
            WeddingId = weddingId,
            Wedding = wedding,
            Events = new List<Event>()
        };

        var guestB = new Guest
        {
            Id = Guid.NewGuid(),
            Name = "Guest B",
            Email = "b@example.com",
            WeddingId = weddingId,
            Wedding = wedding,
            Events = new List<Event>()
        };

        context.Users.Add(user);
        context.Weddings.Add(wedding);
        context.Events.Add(eventEntity);
        context.Guests.AddRange(guestA, guestB);
        context.SaveChanges();

        return (eventEntity, guestA, guestB, wedding);
    }

    private static Guid SeedGuestOnly(WeddingDbContext context, Guid userId)
    {
        var user = new User
        {
            Id = userId,
            UserName = "user@example.com",
            Email = "user@example.com"
        };
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId,
            UserId = userId,
            User = user,
            Title = "Test Wedding",
            Slug = $"test-{weddingId}",
            Location = "Ghent",
            Date = DateTime.UtcNow,
            WeddingUsers = new List<WeddingUser>(),
            Guests = new List<Guest>(),
            Pages = new List<Page>(),
            Media = new List<Media>(),
            Events = new List<Event>()
        };
        var guest = new Guest
        {
            Id = Guid.NewGuid(),
            Name = "Guest A",
            Email = "a@example.com",
            WeddingId = weddingId,
            Wedding = wedding,
            Events = new List<Event>()
        };

        context.Users.Add(user);
        context.Weddings.Add(wedding);
        context.Guests.Add(guest);
        context.SaveChanges();

        return guest.Id;
    }
}
