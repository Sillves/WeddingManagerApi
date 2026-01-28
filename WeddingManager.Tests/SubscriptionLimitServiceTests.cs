using Microsoft.Extensions.Options;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Exceptions;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Tests;

public class SubscriptionLimitServiceTests
{
    [Fact]
    public async Task EnsureGuestLimitAsync_ThrowsWhenLimitReached()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Free);
        var guestRepository = new Mock<IGuestRepository>();
        guestRepository.Setup(r => r.CountByWeddingIdAsync(weddingId)).ReturnsAsync(1);

        var service = CreateService(wedding, guestRepository: guestRepository);

        await Assert.ThrowsAsync<SubscriptionLimitExceededException>(() =>
            service.EnsureGuestLimitAsync(weddingId));
    }

    [Fact]
    public async Task EnsureGuestLimitAsync_AllowsWhenUnlimited()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Pro);
        var guestRepository = new Mock<IGuestRepository>();
        guestRepository.Setup(r => r.CountByWeddingIdAsync(weddingId)).ReturnsAsync(1000);

        var service = CreateService(wedding, guestRepository: guestRepository);

        await service.EnsureGuestLimitAsync(weddingId);
        guestRepository.Verify(r => r.CountByWeddingIdAsync(weddingId), Times.Never);
    }

    [Fact]
    public async Task EnsureGuestLimitAsync_AllowsWhenBelowLimit()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Free);
        var guestRepository = new Mock<IGuestRepository>();
        guestRepository.Setup(r => r.CountByWeddingIdAsync(weddingId)).ReturnsAsync(0);

        var service = CreateService(wedding, guestRepository: guestRepository);

        await service.EnsureGuestLimitAsync(weddingId);
    }

    [Fact]
    public async Task EnsureEventLimitAsync_ThrowsWhenLimitReached()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Free);
        var eventRepository = new Mock<IEventRepository>();
        eventRepository.Setup(r => r.CountByWeddingIdAsync(weddingId)).ReturnsAsync(5);

        var service = CreateService(wedding, eventRepository: eventRepository);

        await Assert.ThrowsAsync<SubscriptionLimitExceededException>(() =>
            service.EnsureEventLimitAsync(weddingId));
    }

    [Fact]
    public async Task EnsureEventLimitAsync_AllowsWhenUnlimited()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Pro);
        var eventRepository = new Mock<IEventRepository>();
        eventRepository.Setup(r => r.CountByWeddingIdAsync(weddingId)).ReturnsAsync(999);

        var service = CreateService(wedding, eventRepository: eventRepository);

        await service.EnsureEventLimitAsync(weddingId);
        eventRepository.Verify(r => r.CountByWeddingIdAsync(weddingId), Times.Never);
    }

    [Fact]
    public async Task EnsureEmailLimitAsync_ThrowsWhenExceeded()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Free);
        var usageRepository = new Mock<ISubscriptionUsageRepository>();
        usageRepository
            .Setup(r => r.GetByPeriodAsync(wedding.UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new SubscriptionUsage { EmailsSent = 4 });

        var service = CreateService(wedding, usageRepository: usageRepository);

        await Assert.ThrowsAsync<SubscriptionLimitExceededException>(() =>
            service.EnsureEmailLimitAsync(weddingId, 2));
    }

    [Fact]
    public async Task EnsureEmailLimitAsync_AllowsWhenUnlimited()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Pro);
        var usageRepository = new Mock<ISubscriptionUsageRepository>();

        var service = CreateService(wedding, usageRepository: usageRepository);

        await service.EnsureEmailLimitAsync(weddingId, 50);

        usageRepository.Verify(r => r.GetByPeriodAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task EnsureEmailLimitAsync_AllowsWhenWithinLimit()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Free);
        var usageRepository = new Mock<ISubscriptionUsageRepository>();
        usageRepository
            .Setup(r => r.GetByPeriodAsync(wedding.UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new SubscriptionUsage { EmailsSent = 2 });

        var service = CreateService(wedding, usageRepository: usageRepository);

        await service.EnsureEmailLimitAsync(weddingId, 3);
    }

    [Fact]
    public async Task EnsureEmailLimitAsync_IgnoresNonPositiveCount()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Free);
        var usageRepository = new Mock<ISubscriptionUsageRepository>();

        var service = CreateService(wedding, usageRepository: usageRepository);

        await service.EnsureEmailLimitAsync(weddingId, 0);

        usageRepository.Verify(r => r.GetByPeriodAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RecordEmailsSentAsync_AddsUsageWhenMissing()
    {
        var userId = Guid.NewGuid();
        var usageRepository = new Mock<ISubscriptionUsageRepository>();
        usageRepository
            .Setup(r => r.GetByPeriodAsync(userId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((SubscriptionUsage?)null);

        var service = CreateService(CreateWedding(Guid.NewGuid(), SubscriptionTier.Free), usageRepository: usageRepository);

        await service.RecordEmailsSentAsync(userId, 3);

        usageRepository.Verify(r => r.AddAsync(It.Is<SubscriptionUsage>(u =>
            u.UserId == userId && u.EmailsSent == 3)), Times.Once);
    }

    [Fact]
    public async Task RecordEmailsSentAsync_IgnoresNonPositiveCount()
    {
        var userId = Guid.NewGuid();
        var usageRepository = new Mock<ISubscriptionUsageRepository>();

        var service = CreateService(CreateWedding(Guid.NewGuid(), SubscriptionTier.Free), usageRepository: usageRepository);

        await service.RecordEmailsSentAsync(userId, 0);

        usageRepository.Verify(r => r.GetByPeriodAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RecordEmailsSentAsync_UpdatesUsageWhenExisting()
    {
        var userId = Guid.NewGuid();
        var usage = new SubscriptionUsage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmailsSent = 2
        };
        var usageRepository = new Mock<ISubscriptionUsageRepository>();
        usageRepository
            .Setup(r => r.GetByPeriodAsync(userId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(usage);

        var service = CreateService(CreateWedding(Guid.NewGuid(), SubscriptionTier.Free), usageRepository: usageRepository);

        await service.RecordEmailsSentAsync(userId, 4);

        usageRepository.Verify(r => r.UpdateAsync(It.Is<SubscriptionUsage>(u =>
            u.Id == usage.Id && u.EmailsSent == 6)), Times.Once);
    }

    [Fact]
    public async Task EnsureGuestLimitAsync_ThrowsWhenWeddingMissing()
    {
        var weddingId = Guid.NewGuid();
        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync((Wedding?)null);

        var service = CreateService(
            CreateWedding(Guid.NewGuid(), SubscriptionTier.Free),
            weddingRepositoryOverride: weddingRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.EnsureGuestLimitAsync(weddingId));
    }

    [Fact]
    public async Task EnsureEventLimitAsync_ThrowsWhenWeddingMissing()
    {
        var weddingId = Guid.NewGuid();
        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync((Wedding?)null);

        var service = CreateService(
            CreateWedding(Guid.NewGuid(), SubscriptionTier.Free),
            weddingRepositoryOverride: weddingRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.EnsureEventLimitAsync(weddingId));
    }

    [Fact]
    public async Task EnsureEmailLimitAsync_ThrowsWhenWeddingMissing()
    {
        var weddingId = Guid.NewGuid();
        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync((Wedding?)null);

        var service = CreateService(
            CreateWedding(Guid.NewGuid(), SubscriptionTier.Free),
            weddingRepositoryOverride: weddingRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.EnsureEmailLimitAsync(weddingId, 1));
    }

    [Fact]
    public async Task EnsureGuestLimitAsync_ThrowsWhenPlanMissing()
    {
        var weddingId = Guid.NewGuid();
        var wedding = CreateWedding(weddingId, SubscriptionTier.Free);

        var service = CreateService(wedding, planOptionsOverride: Options.Create(new SubscriptionPlanOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnsureGuestLimitAsync(weddingId));
    }

    private static SubscriptionLimitService CreateService(
        Wedding wedding,
        Mock<IGuestRepository>? guestRepository = null,
        Mock<IEventRepository>? eventRepository = null,
        Mock<ISubscriptionUsageRepository>? usageRepository = null,
        Mock<IWeddingRepository>? weddingRepositoryOverride = null,
        IOptions<SubscriptionPlanOptions>? planOptionsOverride = null)
    {
        var weddingRepository = weddingRepositoryOverride ?? new Mock<IWeddingRepository>();
        if (weddingRepositoryOverride == null)
        {
            weddingRepository.Setup(r => r.GetByIdAsync(wedding.Id)).ReturnsAsync(wedding);
        }

        guestRepository ??= new Mock<IGuestRepository>();
        eventRepository ??= new Mock<IEventRepository>();
        usageRepository ??= new Mock<ISubscriptionUsageRepository>();

        var options = planOptionsOverride ?? Options.Create(new SubscriptionPlanOptions
        {
            Plans = new Dictionary<string, SubscriptionPlanLimits>
            {
                {
                    SubscriptionTier.Free.ToString(),
                    new SubscriptionPlanLimits
                    {
                        MaxGuests = 1,
                        MaxEvents = 5,
                        MaxEmailsPerMonth = 5
                    }
                },
                {
                    SubscriptionTier.Pro.ToString(),
                    new SubscriptionPlanLimits
                    {
                        MaxGuests = -1,
                        MaxEvents = -1,
                        MaxEmailsPerMonth = -1
                    }
                }
            }
        });

        return new SubscriptionLimitService(
            weddingRepository.Object,
            guestRepository.Object,
            eventRepository.Object,
            usageRepository.Object,
            options);
    }

    private static Wedding CreateWedding(Guid weddingId, SubscriptionTier tier)
    {
        var userId = Guid.NewGuid();
        return new Wedding
        {
            Id = weddingId,
            UserId = userId,
            User = new User
            {
                Id = userId,
                SubscriptionTier = tier
            }
        };
    }
}
