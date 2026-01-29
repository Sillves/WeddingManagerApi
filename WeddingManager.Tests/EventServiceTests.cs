using AutoMapper;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class EventServiceTests
{
    [Fact]
    public async Task CreateEventAsync_SetsWeddingIdAndMapsFields()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        Event? captured = null;
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Event>()))
            .Callback<Event>(e => captured = e)
            .Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock, mapperMock);
        var weddingId = Guid.NewGuid();
        var request = new CreateEventRequestDto
        {
            Name = "Ceremony",
            StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
            Location = "Ghent",
            Description = "Main ceremony"
        };
        var mappedEvent = new Event
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Description = request.Description
        };
        mapperMock.Setup(m => m.Map<Event>(request)).Returns(mappedEvent);
        mapperMock.Setup(m => m.Map<EventDto>(It.IsAny<Event>()))
            .Returns<Event>(e => new EventDto
            {
                Id = e.Id,
                WeddingId = e.WeddingId,
                Name = e.Name,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Description = e.Description
            });

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
        var mapperMock = new Mock<IMapper>();
        var limitMock = new Mock<ISubscriptionLimitService>();
        limitMock.Setup(l => l.EnsureEventLimitAsync(It.IsAny<Guid>())).ReturnsAsync(Result.Ok());

        var service = CreateService(repositoryMock, mapperMock, limitMock);
        var weddingId = Guid.NewGuid();
        var request = CreateValidCreateRequest();
        var mappedEvent = new Event
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Description = request.Description
        };
        mapperMock.Setup(m => m.Map<Event>(request)).Returns(mappedEvent);
        mapperMock.Setup(m => m.Map<EventDto>(It.IsAny<Event>()))
            .Returns(new EventDto { Id = Guid.NewGuid(), WeddingId = weddingId, Name = request.Name });

        var result = await service.CreateEventAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        limitMock.Verify(l => l.EnsureEventLimitAsync(weddingId), Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_UpdatesFields()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
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
        var service = CreateService(repositoryMock, mapperMock);

        var update = new UpdateEventRequestDto
        {
            Name = "Reception",
            StartDate = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 22, 0, 0, DateTimeKind.Utc),
            Location = "Bruges",
            Description = "Updated details"
        };
        mapperMock.Setup(m => m.Map(update, existing))
            .Callback<UpdateEventRequestDto, Event>((src, dest) =>
            {
                dest.Name = src.Name;
                dest.StartDate = src.StartDate;
                dest.EndDate = src.EndDate;
                dest.Location = src.Location;
                dest.Description = src.Description;
            })
            .Returns(existing);
        mapperMock.Setup(m => m.Map<EventDto>(It.IsAny<Event>()))
            .Returns<Event>(e => new EventDto
            {
                Id = e.Id,
                WeddingId = e.WeddingId,
                Name = e.Name,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Description = e.Description
            });

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
        var mapperMock = new Mock<IMapper>();
        repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);
        var service = CreateService(repositoryMock, mapperMock);

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
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(repositoryMock, mapperMock);
        var request = CreateValidCreateRequest();
        request.Name = name;
        request.Location = location;

        var result = await service.CreateEventAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == message);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
        mapperMock.Verify(m => m.Map<Event>(It.IsAny<CreateEventRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ThrowsWhenStartDateDefault()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(repositoryMock, mapperMock);
        var request = CreateValidCreateRequest();
        request.StartDate = default;

        var result = await service.CreateEventAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Event start date is required");
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ThrowsWhenEndDateDefault()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(repositoryMock, mapperMock);
        var request = CreateValidCreateRequest();
        request.EndDate = default;

        var result = await service.CreateEventAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Event end date is required");
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ThrowsWhenEndDateBeforeStartDate()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(repositoryMock, mapperMock);
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
        var mapperMock = new Mock<IMapper>();
        var service = CreateService(repositoryMock, mapperMock);
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
        var mapperMock = new Mock<IMapper>();
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
        mapperMock.Setup(m => m.Map<IEnumerable<EventDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns<IEnumerable<Event>>(src => src.Select(e => new EventDto
            {
                Id = e.Id,
                WeddingId = e.WeddingId,
                Name = e.Name,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Description = e.Description
            }).ToList());
        var service = CreateService(repositoryMock, mapperMock);

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
        var mapperMock = new Mock<IMapper>();
        repositoryMock
            .Setup(r => r.AddGuestToEventAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(EventGuestChangeResult.Unauthorized);
        var service = CreateService(repositoryMock, mapperMock);

        var result = await service.AddGuestToEventAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Forbidden, result.Errors[0].Code);
    }

    [Fact]
    public async Task AddGuestsToEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        var expected = new EventGuestBatchChangeResultDto
        {
            Status = EventGuestChangeResult.Added,
            AddedGuestIds = [Guid.NewGuid()]
        };
        repositoryMock
            .Setup(r => r.AddGuestsToEventAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(expected);
        var service = CreateService(repositoryMock, mapperMock);

        var result = await service.AddGuestsToEventAsync(Guid.NewGuid(), [Guid.NewGuid()]);

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
    }

    [Fact]
    public async Task RemoveGuestFromEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        repositoryMock
            .Setup(r => r.RemoveGuestFromEventAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(EventGuestChangeResult.NotInEvent);
        var service = CreateService(repositoryMock, mapperMock);

        var result = await service.RemoveGuestFromEventAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task RemoveGuestsFromEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        var expected = new EventGuestBatchRemoveResultDto
        {
            Status = EventGuestChangeResult.Removed,
            RemovedGuestIds = [Guid.NewGuid()]
        };
        repositoryMock
            .Setup(r => r.RemoveGuestsFromEventAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(expected);
        var service = CreateService(repositoryMock, mapperMock);

        var result = await service.RemoveGuestsFromEventAsync(Guid.NewGuid(), [Guid.NewGuid()]);

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
    }

    private static EventService CreateService(
        Mock<IEventRepository> repositoryMock,
        Mock<IMapper> mapperMock,
        Mock<ISubscriptionLimitService>? limitMock = null)
    {
        limitMock ??= new Mock<ISubscriptionLimitService>();
        limitMock
            .Setup(l => l.EnsureEventLimitAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok());
        return new EventService(repositoryMock.Object, limitMock.Object, mapperMock.Object);
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
