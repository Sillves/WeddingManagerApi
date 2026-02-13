using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class WeddingServiceTests
{
    private static WeddingService CreateService(
        Mock<IWeddingRepository>? repositoryMock = null,
        Mock<IUserContextService>? userContextMock = null,
        Mock<IWeddingUserRepository>? weddingUserRepoMock = null)
    {
        repositoryMock ??= new Mock<IWeddingRepository>();
        userContextMock ??= new Mock<IUserContextService>();
        weddingUserRepoMock ??= new Mock<IWeddingUserRepository>();
        return new WeddingService(repositoryMock.Object, userContextMock.Object, weddingUserRepoMock.Object);
    }

    [Fact]
    public async Task AddAsync_SetsIdSlugAndUserIdWhenMissing()
    {
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.AddAsync(It.IsAny<Wedding>())).Returns(Task.CompletedTask);
        var userContextMock = new Mock<IUserContextService>();
        var userId = Guid.NewGuid();
        userContextMock.Setup(s => s.GetUserId()).Returns(userId);
        var weddingUserRepoMock = new Mock<IWeddingUserRepository>();
        weddingUserRepoMock.Setup(r => r.AddAsync(It.IsAny<WeddingUser>())).Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock, userContextMock, weddingUserRepoMock);
        var wedding = new Wedding
        {
            Id = Guid.NewGuid(),
            Title = "  Summer Bash!! 2026  ",
            UserId = Guid.Empty
        };
        var originalId = wedding.Id;

        var result = await service.AddAsync(wedding);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(originalId, wedding.Id);
        Assert.NotEqual(Guid.Empty, wedding.Id);
        Assert.Equal(userId, wedding.UserId);
        Assert.Equal("summer-bash-2026", wedding.Slug);
        repositoryMock.Verify(r => r.AddAsync(wedding), Times.Once);
    }

    [Fact]
    public async Task AddAsync_UsesExistingUserId()
    {
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.AddAsync(It.IsAny<Wedding>())).Returns(Task.CompletedTask);
        var userContextMock = new Mock<IUserContextService>();
        userContextMock.Setup(s => s.GetUserId()).Returns(Guid.NewGuid());
        var weddingUserRepoMock = new Mock<IWeddingUserRepository>();
        weddingUserRepoMock.Setup(r => r.AddAsync(It.IsAny<WeddingUser>())).Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock, userContextMock, weddingUserRepoMock);
        var existingUserId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Title = "Test",
            UserId = existingUserId
        };

        var result = await service.AddAsync(wedding);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingUserId, wedding.UserId);
        userContextMock.Verify(s => s.GetUserId(), Times.Never);
    }

    [Fact]
    public async Task AddAsync_CreatesOwnerWeddingUser()
    {
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.AddAsync(It.IsAny<Wedding>())).Returns(Task.CompletedTask);
        var userContextMock = new Mock<IUserContextService>();
        userContextMock.Setup(u => u.GetUserId()).Returns(userId);
        var weddingUserRepoMock = new Mock<IWeddingUserRepository>();
        WeddingUser? capturedWu = null;
        weddingUserRepoMock.Setup(r => r.AddAsync(It.IsAny<WeddingUser>()))
            .Callback<WeddingUser>(wu => capturedWu = wu).Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock, userContextMock, weddingUserRepoMock);

        var wedding = new Wedding { Title = "Test Wedding" };
        await service.AddAsync(wedding);

        Assert.NotNull(capturedWu);
        Assert.Equal(wedding.Id, capturedWu!.WeddingId);
        Assert.Equal(userId, capturedWu.UserId);
        Assert.Equal(WeddingUserRole.Owner, capturedWu.Role);
        Assert.True(capturedWu.CanAccessGuests);
        Assert.True(capturedWu.CanAccessEvents);
        Assert.True(capturedWu.CanAccessExpenses);
        Assert.True(capturedWu.CanAccessWebsite);
        Assert.False(capturedWu.IsReadOnly);
        weddingUserRepoMock.Verify(r => r.AddAsync(It.IsAny<WeddingUser>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_UsesUserContext()
    {
        var repositoryMock = new Mock<IWeddingRepository>();
        var userContextMock = new Mock<IUserContextService>();
        var userId = Guid.NewGuid();
        userContextMock.Setup(s => s.GetUserId()).Returns(userId);
        var weddings = new List<Wedding>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Title = "Test Wedding" }
        };
        repositoryMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(weddings);
        var service = CreateService(repositoryMock, userContextMock);

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Same(weddings, result.Value);
        repositoryMock.Verify(r => r.GetAllAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAllWithRoleAsync_ReturnsDtosWithPermissions()
    {
        var userId = Guid.NewGuid();
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId, Title = "Test", Slug = "test", Date = DateTime.UtcNow,
            Location = "Amsterdam", Guests = new List<Guest> { new() { Name = "G1", Email = "g@test.com" } }
        };
        var weddingUser = new WeddingUser
        {
            WeddingId = weddingId, UserId = userId, Role = WeddingUserRole.Planner,
            CanAccessGuests = true, CanAccessEvents = false, CanAccessExpenses = true,
            CanAccessWebsite = false, IsReadOnly = true, Wedding = wedding
        };
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.GetAllWithRoleAsync(userId))
            .ReturnsAsync(new List<(Wedding, WeddingUser)> { (wedding, weddingUser) });
        var userContextMock = new Mock<IUserContextService>();
        userContextMock.Setup(s => s.GetUserId()).Returns(userId);
        var service = CreateService(repositoryMock, userContextMock);

        var result = await service.GetAllWithRoleAsync();

        Assert.True(result.IsSuccess);
        var dtos = result.Value!.ToList();
        Assert.Single(dtos);
        Assert.Equal(weddingId, dtos[0].Id);
        Assert.Equal("Test", dtos[0].Title);
        Assert.Equal(WeddingUserRole.Planner, dtos[0].Role);
        Assert.Equal(1, dtos[0].GuestCount);
        Assert.True(dtos[0].CanAccessGuests);
        Assert.False(dtos[0].CanAccessEvents);
        Assert.True(dtos[0].CanAccessExpenses);
        Assert.False(dtos[0].CanAccessWebsite);
        Assert.True(dtos[0].IsReadOnly);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsWeddingWhenFound()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding { Id = weddingId, Title = "Found Wedding" };
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var service = CreateService(repositoryMock);

        var result = await service.GetByIdAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.Same(wedding, result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync((Wedding?)null);
        var service = CreateService(repositoryMock);

        var result = await service.GetByIdAsync(weddingId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task GetByIdOrSlugAsync_ReturnsWeddingWhenFound()
    {
        var wedding = new Wedding { Id = Guid.NewGuid(), Title = "Slug Wedding", Slug = "slug-wedding" };
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.GetByIdOrSlugAsync("slug-wedding")).ReturnsAsync(wedding);
        var service = CreateService(repositoryMock);

        var result = await service.GetByIdOrSlugAsync("slug-wedding");

        Assert.True(result.IsSuccess);
        Assert.Same(wedding, result.Value);
    }

    [Fact]
    public async Task GetByIdOrSlugAsync_ReturnsNotFoundWhenMissing()
    {
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.GetByIdOrSlugAsync("ghost")).ReturnsAsync((Wedding?)null);
        var service = CreateService(repositoryMock);

        var result = await service.GetByIdOrSlugAsync("ghost");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        var wedding = new Wedding { Id = Guid.NewGuid(), Title = "Updated" };
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.UpdateAsync(wedding)).Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock);

        var result = await service.UpdateAsync(wedding);

        Assert.True(result.IsSuccess);
        repositoryMock.Verify(r => r.UpdateAsync(wedding), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        var weddingId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.DeleteAsync(weddingId)).Returns(Task.CompletedTask);
        var service = CreateService(repositoryMock);

        var result = await service.DeleteAsync(weddingId);

        Assert.True(result.IsSuccess);
        repositoryMock.Verify(r => r.DeleteAsync(weddingId), Times.Once);
    }
}
