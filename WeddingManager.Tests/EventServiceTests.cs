using Moq;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class EventServiceTests
{
    private readonly ApplicationMapper _mapper = new();

    [Fact]
    public async Task CreateEventAsync_SetsWeddingIdAndMapsFields()
    {
        var repositoryMock = new Mock<IEventRepository>();
        Event? captured = null;
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Event>()))
            .Callback<Event>(e => captured = e)
            .Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock);
        var weddingId = Guid.NewGuid();
        var request = new CreateEventRequestDto
        {
            Name = "Ceremony",
            StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
            Location = "Ghent",
            Description = "Main ceremony"
        };

        var result = await service.CreateEventAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal(weddingId, dto.WeddingId);
        Assert.Equal(request.Name, dto.Name);
        Assert.Equal(request.Location, dto.Location);
        Assert.Equal(request.Description, dto.Description);
        Assert.NotNull(captured);
        Assert.Equal(weddingId, captured!.WeddingId);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_EnforcesLimit()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var limitMock = new Mock<ISubscriptionLimitService>();
        limitMock.Setup(l => l.EnsureEventLimitAsync(It.IsAny<Guid>())).ReturnsAsync(Result.Ok());

        var service = CreateService(repositoryMock, limitMock);
        var weddingId = Guid.NewGuid();
        var request = CreateValidCreateRequest();

        var result = await service.CreateEventAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        limitMock.Verify(l => l.EnsureEventLimitAsync(weddingId), Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_UpdatesFields()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var existing = new Event
        {
            Id = Guid.NewGuid(),
            WeddingId = Guid.NewGuid(),
            Name = "Ceremony",
            StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
            Location = "Ghent"
        };
        repositoryMock.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock);

        var update = new UpdateEventRequestDto
        {
            Name = "Reception",
            StartDate = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 22, 0, 0, DateTimeKind.Utc),
            Location = "Bruges",
            Description = "Updated details"
        };

        var result = await service.UpdateEventAsync(existing.Id, update);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(update.Name, dto.Name);
        Assert.Equal(update.Location, dto.Location);
        Assert.Equal(update.Description, dto.Description);
        Assert.Equal(update.StartDate, dto.StartDate);
        Assert.Equal(update.EndDate, dto.EndDate);
        repositoryMock.Verify(r => r.UpdateAsync(It.Is<Event>(e =>
            e.Id == existing.Id &&
            e.Name == update.Name &&
            e.Location == update.Location &&
            e.Description == update.Description &&
            e.StartDate == update.StartDate &&
            e.EndDate == update.EndDate)), Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_ThrowsWhenMissing()
    {
        var repositoryMock = new Mock<IEventRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);
        var service = CreateService(repositoryMock);

        var update = new UpdateEventRequestDto
        {
            Name = "Reception",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(2),
            Location = "Bruges"
        };

        var result = await service.UpdateEventAsync(Guid.NewGuid(), update);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Location", "Event name is required")]
    [InlineData("Name", "", "Event location is required")]
    public async Task CreateEventAsync_ThrowsWhenRequiredFieldsMissing(string name, string location, string message)
    {
        var repositoryMock = new Mock<IEventRepository>();
        var service = CreateService(repositoryMock);
        var request = CreateValidCreateRequest();
        request.Name = name;
        request.Location = location;

        var result = await service.CreateEventAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == message);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ThrowsWhenStartDateDefault()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var service = CreateService(repositoryMock);
        var request = CreateValidCreateRequest();
        request.StartDate = default;

        var result = await service.CreateEventAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Event start date is required");
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ThrowsWhenEndDateBeforeStartDate()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var service = CreateService(repositoryMock);
        var request = CreateValidCreateRequest();
        request.StartDate = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        request.EndDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var result = await service.CreateEventAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Event end date must be on or after start date");
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEventAsync_ThrowsWhenEndDateBeforeStartDate()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var service = CreateService(repositoryMock);
        var update = CreateValidUpdateRequest();
        update.StartDate = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        update.EndDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var result = await service.UpdateEventAsync(Guid.NewGuid(), update);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Event end date must be on or after start date");
        repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task GetByWeddingIdAsync_ReturnsEventsForWedding()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var weddingId = Guid.NewGuid();
        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                Name = "Ceremony",
                StartDate = DateTime.UtcNow,
                Location = "Ghent"
            },
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = Guid.NewGuid(),
                Name = "Other",
                StartDate = DateTime.UtcNow,
                Location = "Antwerp"
            }
        };
        repositoryMock.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(events.Where(e => e.WeddingId == weddingId));
        var service = CreateService(repositoryMock);

        var result = await service.GetByWeddingIdAsync(weddingId);

        Assert.True(result.IsSuccess);
        var list = result.Value!.ToList();
        Assert.Single(list);
        Assert.Equal(weddingId, list[0].WeddingId);
    }

    [Fact]
    public async Task AddGuestToEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        repositoryMock
            .Setup(r => r.AddGuestToEventAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(EventGuestChangeResult.Unauthorized);
        var service = CreateService(repositoryMock);

        var result = await service.AddGuestToEventAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Forbidden, result.Errors[0].Code);
    }

    [Fact]
    public async Task AddGuestsToEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var expected = new EventGuestBatchChangeResultDto
        {
            Status = EventGuestChangeResult.Added,
            AddedGuestIds = [Guid.NewGuid()]
        };
        repositoryMock
            .Setup(r => r.AddGuestsToEventAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(expected);
        var service = CreateService(repositoryMock);

        var result = await service.AddGuestsToEventAsync(Guid.NewGuid(), [Guid.NewGuid()]);

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
    }

    [Fact]
    public async Task RemoveGuestFromEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        repositoryMock
            .Setup(r => r.RemoveGuestFromEventAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(EventGuestChangeResult.NotInEvent);
        var service = CreateService(repositoryMock);

        var result = await service.RemoveGuestFromEventAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task RemoveGuestsFromEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var expected = new EventGuestBatchRemoveResultDto
        {
            Status = EventGuestChangeResult.Removed,
            RemovedGuestIds = [Guid.NewGuid()]
        };
        repositoryMock
            .Setup(r => r.RemoveGuestsFromEventAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(expected);
        var service = CreateService(repositoryMock);

        var result = await service.RemoveGuestsFromEventAsync(Guid.NewGuid(), [Guid.NewGuid()]);

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
    }

    private EventService CreateService(
        Mock<IEventRepository> repositoryMock,
        Mock<ISubscriptionLimitService>? limitMock = null)
    {
        limitMock ??= new Mock<ISubscriptionLimitService>();
        limitMock
            .Setup(l => l.EnsureEventLimitAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok());
        return new EventService(repositoryMock.Object, limitMock.Object, _mapper);
    }

    private static CreateEventRequestDto CreateValidCreateRequest() => new()
    {
        Name = "Ceremony",
        StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
        Location = "Ghent",
        Description = "Main ceremony"
    };

    private static UpdateEventRequestDto CreateValidUpdateRequest() => new()
    {
        Name = "Reception",
        StartDate = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 1, 1, 22, 0, 0, DateTimeKind.Utc),
        Location = "Bruges",
        Description = "Updated details"
    };
}
