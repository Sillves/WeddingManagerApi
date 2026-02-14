using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Tests;

public class ReferralServiceTests
{
    private static readonly Guid ReferrerUserId = Guid.NewGuid();
    private static readonly Guid ReferredUserId = Guid.NewGuid();

    private static ReferralService CreateService(
        Mock<IReferralRepository>? referralRepoMock = null,
        Mock<UserManager<User>>? userManagerMock = null)
    {
        referralRepoMock ??= new Mock<IReferralRepository>();
        userManagerMock ??= CreateUserManager();
        var logger = new Mock<ILogger<ReferralService>>();
        return new ReferralService(referralRepoMock.Object, userManagerMock.Object, logger.Object);
    }

    private static Mock<UserManager<User>> CreateUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static User CreateReferrerUser(string? referralCode = null)
    {
        return new User
        {
            Id = ReferrerUserId,
            FirstName = "Margot",
            LastName = "Laporte",
            Email = "margot@example.com",
            UserName = "margot@example.com",
            ReferralCode = referralCode,
        };
    }

    [Fact]
    public async Task GetOrCreateReferralCodeAsync_ReturnsExistingCode()
    {
        var userManager = CreateUserManager();
        var user = CreateReferrerUser("margot-laporte-abc123");
        userManager.Setup(um => um.FindByIdAsync(ReferrerUserId.ToString())).ReturnsAsync(user);

        var service = CreateService(userManagerMock: userManager);
        var result = await service.GetOrCreateReferralCodeAsync(ReferrerUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal("margot-laporte-abc123", result.Value);
        userManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateReferralCodeAsync_GeneratesNewCode()
    {
        var userManager = CreateUserManager();
        var user = CreateReferrerUser(null);
        userManager.Setup(um => um.FindByIdAsync(ReferrerUserId.ToString())).ReturnsAsync(user);
        userManager.Setup(um => um.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        var service = CreateService(userManagerMock: userManager);
        var result = await service.GetOrCreateReferralCodeAsync(ReferrerUserId);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("margot-laporte-", result.Value);
        userManager.Verify(um => um.UpdateAsync(It.Is<User>(u => u.ReferralCode != null)), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateReferralCodeAsync_ReturnsNotFoundForMissingUser()
    {
        var userManager = CreateUserManager();
        userManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var service = CreateService(userManagerMock: userManager);
        var result = await service.GetOrCreateReferralCodeAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetStatsAsync_CalculatesCorrectly()
    {
        var userManager = CreateUserManager();
        var user = CreateReferrerUser("margot-laporte-abc123");
        userManager.Setup(um => um.FindByIdAsync(ReferrerUserId.ToString())).ReturnsAsync(user);

        var referrals = new List<Referral>
        {
            new() { Id = Guid.NewGuid(), ReferrerUserId = ReferrerUserId, RegisteredAt = DateTime.UtcNow, ConvertedAt = DateTime.UtcNow, CommissionPercentage = 15m, CommissionStatus = CommissionStatus.Paid },
            new() { Id = Guid.NewGuid(), ReferrerUserId = ReferrerUserId, RegisteredAt = DateTime.UtcNow, ConvertedAt = null, CommissionPercentage = 15m, CommissionStatus = CommissionStatus.Pending },
            new() { Id = Guid.NewGuid(), ReferrerUserId = ReferrerUserId, RegisteredAt = DateTime.UtcNow, ConvertedAt = DateTime.UtcNow, CommissionPercentage = 15m, CommissionStatus = CommissionStatus.Pending },
        };

        var repoMock = new Mock<IReferralRepository>();
        repoMock.Setup(r => r.GetByReferrerUserIdAsync(ReferrerUserId)).ReturnsAsync(referrals);

        var service = CreateService(referralRepoMock: repoMock, userManagerMock: userManager);
        var result = await service.GetStatsAsync(ReferrerUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.SignedUpCount);
        Assert.Equal(2, result.Value.SubscribedCount);
        Assert.Equal(66.7m, result.Value.ConversionRate);
        Assert.Equal(15m, result.Value.TotalCommissionEarned); // only paid
        Assert.Equal(15m, result.Value.PendingPayout); // pending with conversion
    }

    [Fact]
    public async Task TrackReferralRegistrationAsync_CreatesReferralRecord()
    {
        var userManager = CreateUserManager();
        var referrer = CreateReferrerUser("margot-laporte-abc123");
        userManager.Setup(um => um.Users).Returns(new List<User> { referrer }.AsQueryable());

        var repoMock = new Mock<IReferralRepository>();
        repoMock.Setup(r => r.GetByReferredUserIdAsync(ReferredUserId)).ReturnsAsync((Referral?)null);

        var service = CreateService(referralRepoMock: repoMock, userManagerMock: userManager);
        var result = await service.TrackReferralRegistrationAsync("margot-laporte-abc123", ReferredUserId);

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.AddAsync(It.Is<Referral>(ref_ =>
            ref_.ReferrerUserId == ReferrerUserId &&
            ref_.ReferredUserId == ReferredUserId &&
            ref_.ReferralCode == "margot-laporte-abc123"
        )), Times.Once);
    }

    [Fact]
    public async Task TrackReferralRegistrationAsync_IgnoresInvalidCode()
    {
        var userManager = CreateUserManager();
        userManager.Setup(um => um.Users).Returns(new List<User>().AsQueryable());

        var repoMock = new Mock<IReferralRepository>();

        var service = CreateService(referralRepoMock: repoMock, userManagerMock: userManager);
        var result = await service.TrackReferralRegistrationAsync("nonexistent-code", ReferredUserId);

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Referral>()), Times.Never);
    }

    [Fact]
    public async Task TrackReferralRegistrationAsync_PreventsSelfReferral()
    {
        var userManager = CreateUserManager();
        var referrer = CreateReferrerUser("margot-laporte-abc123");
        userManager.Setup(um => um.Users).Returns(new List<User> { referrer }.AsQueryable());

        var repoMock = new Mock<IReferralRepository>();

        var service = CreateService(referralRepoMock: repoMock, userManagerMock: userManager);
        var result = await service.TrackReferralRegistrationAsync("margot-laporte-abc123", ReferrerUserId);

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Referral>()), Times.Never);
    }

    [Fact]
    public async Task TrackReferralConversionAsync_UpdatesReferral()
    {
        var referral = new Referral
        {
            Id = Guid.NewGuid(),
            ReferrerUserId = ReferrerUserId,
            ReferredUserId = ReferredUserId,
            RegisteredAt = DateTime.UtcNow.AddDays(-5),
            ConvertedAt = null,
            CommissionStatus = CommissionStatus.Pending,
        };

        var repoMock = new Mock<IReferralRepository>();
        repoMock.Setup(r => r.GetByReferredUserIdAsync(ReferredUserId)).ReturnsAsync(referral);

        var service = CreateService(referralRepoMock: repoMock);
        var result = await service.TrackReferralConversionAsync(ReferredUserId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(referral.ConvertedAt);
        repoMock.Verify(r => r.UpdateAsync(referral), Times.Once);
    }

    [Fact]
    public async Task TrackReferralConversionAsync_SkipsAlreadyConverted()
    {
        var referral = new Referral
        {
            Id = Guid.NewGuid(),
            ReferrerUserId = ReferrerUserId,
            ReferredUserId = ReferredUserId,
            ConvertedAt = DateTime.UtcNow.AddDays(-1),
        };

        var repoMock = new Mock<IReferralRepository>();
        repoMock.Setup(r => r.GetByReferredUserIdAsync(ReferredUserId)).ReturnsAsync(referral);

        var service = CreateService(referralRepoMock: repoMock);
        var result = await service.TrackReferralConversionAsync(ReferredUserId);

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Referral>()), Times.Never);
    }

    [Fact]
    public void GenerateReferralCode_CreatesSlugFromName()
    {
        var user = new User { FirstName = "Margot", LastName = "Laporte" };
        var code = ReferralService.GenerateReferralCode(user);

        Assert.StartsWith("margot-laporte-", code);
        Assert.Equal("margot-laporte-".Length + 6, code.Length);
    }

    [Fact]
    public void GenerateReferralCode_HandlesDiacritics()
    {
        var user = new User { FirstName = "René", LastName = "Müller" };
        var code = ReferralService.GenerateReferralCode(user);

        Assert.StartsWith("rene-muller-", code);
    }
}
