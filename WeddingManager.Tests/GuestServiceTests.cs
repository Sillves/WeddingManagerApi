using AutoMapper;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class GuestServiceTests
{
    [Fact]
    public async Task CreateGuestAsync_EnforcesLimitAndAddsGuest()
    {
        var weddingId = Guid.NewGuid();
        var repositoryMock = new Mock<IGuestRepository>();
        repositoryMock.Setup(r => r.GetByEmailAsync(weddingId, It.IsAny<string>()))
            .ReturnsAsync((Guest?)null);
        repositoryMock.Setup(r => r.AddAsync(It.IsAny<Guest>()))
            .Returns(Task.CompletedTask);

        var weddingRepository = new Mock<IWeddingRepository>();
        var emailService = new Mock<IEmailService>();
        var limitService = new Mock<ISubscriptionLimitService>();
        limitService.Setup(l => l.EnsureGuestLimitAsync(weddingId)).ReturnsAsync(Result.Ok());

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<GuestDto>(It.IsAny<Guest>()))
            .Returns<Guest>(g => new GuestDto
            {
                Id = g.Id,
                WeddingId = g.WeddingId,
                Name = g.Name,
                Email = g.Email,
                RsvpStatus = g.RsvpStatus,
                PreferredLanguage = g.PreferredLanguage
            });

        var service = new GuestService(
            repositoryMock.Object,
            weddingRepository.Object,
            emailService.Object,
            limitService.Object,
            mapperMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<GuestService>>());

        var request = new CreateGuestRequestDto
        {
            Name = "Jamie",
            Email = "jamie@example.com",
            RsvpStatus = RsvpStatus.Pending,
            PreferredLanguage = "en"
        };

        var result = await service.CreateGuestAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        limitService.Verify(l => l.EnsureGuestLimitAsync(weddingId), Times.Once);
        repositoryMock.Verify(r => r.AddAsync(It.Is<Guest>(g =>
            g.WeddingId == weddingId && g.Email == request.Email)), Times.Once);
        Assert.Equal(request.Email, result.Value!.Email);
    }

    [Fact]
    public async Task SendInvitationAsync_EnforcesEmailLimitAndRecordsUsage()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId,
            UserId = userId,
            User = new User { Id = userId }
        };
        var guestId = Guid.NewGuid();
        var guest = new Guest
        {
            Id = guestId,
            WeddingId = weddingId,
            Name = "Jamie",
            Email = "jamie@example.com",
            InvitationToken = "token",
            InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var repositoryMock = new Mock<IGuestRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync(guest);
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Guest>())).Returns(Task.CompletedTask);

        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);

        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendInvitationAsync(guest, wedding)).Returns(Task.CompletedTask);

        var limitService = new Mock<ISubscriptionLimitService>();
        limitService.Setup(l => l.EnsureEmailLimitAsync(weddingId, 1)).ReturnsAsync(Result.Ok());
        limitService.Setup(l => l.RecordEmailsSentAsync(userId, 1)).ReturnsAsync(Result.Ok());

        var mapperMock = new Mock<IMapper>();

        var service = new GuestService(
            repositoryMock.Object,
            weddingRepository.Object,
            emailService.Object,
            limitService.Object,
            mapperMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<GuestService>>());

        var result = await service.SendInvitationAsync(weddingId, guestId);

        Assert.True(result.IsSuccess);
        limitService.Verify(l => l.EnsureEmailLimitAsync(weddingId, 1), Times.Once);
        limitService.Verify(l => l.RecordEmailsSentAsync(userId, 1), Times.Once);
        emailService.Verify(e => e.SendInvitationAsync(guest, wedding), Times.Once);
    }

    [Fact]
    public async Task SendInvitationsAsync_EnforcesLimitAndRecordsBatchUsage()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId,
            UserId = userId,
            User = new User { Id = userId }
        };
        var guests = new List<Guest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                Name = "Jamie",
                Email = "jamie@example.com",
                RsvpStatus = RsvpStatus.Pending,
                InvitationToken = "token-1",
                InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                Name = "Alex",
                Email = "alex@example.com",
                RsvpStatus = RsvpStatus.Pending,
                InvitationToken = "token-2",
                InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1)
            }
        };

        var repositoryMock = new Mock<IGuestRepository>();
        repositoryMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(guests);
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Guest>())).Returns(Task.CompletedTask);

        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);

        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendInvitationAsync(It.IsAny<Guest>(), wedding))
            .Returns(Task.CompletedTask);

        var limitService = new Mock<ISubscriptionLimitService>();
        limitService.Setup(l => l.EnsureEmailLimitAsync(weddingId, guests.Count)).ReturnsAsync(Result.Ok());
        limitService.Setup(l => l.RecordEmailsSentAsync(userId, guests.Count)).ReturnsAsync(Result.Ok());

        var mapperMock = new Mock<IMapper>();

        var service = new GuestService(
            repositoryMock.Object,
            weddingRepository.Object,
            emailService.Object,
            limitService.Object,
            mapperMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<GuestService>>());

        var result = await service.SendInvitationsAsync(weddingId, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(guests.Count, result.Value!.SentCount);
        limitService.Verify(l => l.EnsureEmailLimitAsync(weddingId, guests.Count), Times.Once);
        limitService.Verify(l => l.RecordEmailsSentAsync(userId, guests.Count), Times.Once);
    }

    [Fact]
    public async Task SendInvitationsAsync_RecordsOnlySuccessfulSends()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId,
            UserId = userId,
            User = new User { Id = userId }
        };
        var guests = new List<Guest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                Name = "Jamie",
                Email = "jamie@example.com",
                RsvpStatus = RsvpStatus.Pending,
                InvitationToken = "token-1",
                InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                Name = "Alex",
                Email = "alex@example.com",
                RsvpStatus = RsvpStatus.Pending,
                InvitationToken = "token-2",
                InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1)
            }
        };

        var repositoryMock = new Mock<IGuestRepository>();
        repositoryMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(guests);
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Guest>())).Returns(Task.CompletedTask);

        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);

        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendInvitationAsync(guests[0], wedding))
            .Returns(Task.CompletedTask);
        emailService.Setup(e => e.SendInvitationAsync(guests[1], wedding))
            .ThrowsAsync(new InvalidOperationException("Boom"));

        var limitService = new Mock<ISubscriptionLimitService>();
        limitService.Setup(l => l.EnsureEmailLimitAsync(weddingId, guests.Count)).ReturnsAsync(Result.Ok());
        limitService.Setup(l => l.RecordEmailsSentAsync(userId, 1)).ReturnsAsync(Result.Ok());

        var mapperMock = new Mock<IMapper>();

        var service = new GuestService(
            repositoryMock.Object,
            weddingRepository.Object,
            emailService.Object,
            limitService.Object,
            mapperMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<GuestService>>());

        var result = await service.SendInvitationsAsync(weddingId, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.SentCount);
        Assert.Equal(1, result.Value!.FailedCount);
        limitService.Verify(l => l.RecordEmailsSentAsync(userId, 1), Times.Once);
    }
}
