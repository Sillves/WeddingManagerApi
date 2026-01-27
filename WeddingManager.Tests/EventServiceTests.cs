using AutoMapper;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;

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
        var service = new EventService(repositoryMock.Object, mapperMock.Object);
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

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(weddingId, result.WeddingId);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Location, result.Location);
        Assert.Equal(request.Description, result.Description);
        Assert.NotNull(captured);
        Assert.Equal(weddingId, captured!.WeddingId);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
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
        var service = new EventService(repositoryMock.Object, mapperMock.Object);

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

        Assert.Equal(update.Name, result.Name);
        Assert.Equal(update.Location, result.Location);
        Assert.Equal(update.Description, result.Description);
        Assert.Equal(update.StartDate, result.StartDate);
        Assert.Equal(update.EndDate, result.EndDate);
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
        var service = new EventService(repositoryMock.Object, mapperMock.Object);

        var update = new UpdateEventRequestDto
        {
            Name = "Reception",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(2),
            Location = "Bruges"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateEventAsync(Guid.NewGuid(), update));
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
        var service = new EventService(repositoryMock.Object, mapperMock.Object);

        var result = (await service.GetByWeddingIdAsync(weddingId)).ToList();

        Assert.Single(result);
        Assert.Equal(weddingId, result[0].WeddingId);
    }

    [Fact]
    public async Task AddGuestToEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        repositoryMock
            .Setup(r => r.AddGuestToEventAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(EventGuestChangeResult.Unauthorized);
        var service = new EventService(repositoryMock.Object, mapperMock.Object);

        var result = await service.AddGuestToEventAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(EventGuestChangeResult.Unauthorized, result);
    }

    [Fact]
    public async Task RemoveGuestFromEventAsync_DelegatesResult()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var mapperMock = new Mock<IMapper>();
        repositoryMock
            .Setup(r => r.RemoveGuestFromEventAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(EventGuestChangeResult.NotInEvent);
        var service = new EventService(repositoryMock.Object, mapperMock.Object);

        var result = await service.RemoveGuestFromEventAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(EventGuestChangeResult.NotInEvent, result);
    }
}
