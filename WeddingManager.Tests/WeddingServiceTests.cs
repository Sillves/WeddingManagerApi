using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class WeddingServiceTests
{
    [Fact]
    public async Task AddAsync_SetsIdSlugAndUserIdWhenMissing()
    {
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.AddAsync(It.IsAny<Wedding>())).Returns(Task.CompletedTask);
        var userContextMock = new Mock<IUserContextService>();
        var userId = Guid.NewGuid();
        userContextMock.Setup(s => s.GetUserId()).Returns(userId);
        var service = new WeddingService(repositoryMock.Object, userContextMock.Object);
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
        var service = new WeddingService(repositoryMock.Object, userContextMock.Object);
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
        var service = new WeddingService(repositoryMock.Object, userContextMock.Object);

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Same(weddings, result.Value);
        repositoryMock.Verify(r => r.GetAllAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsWeddingWhenFound()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding { Id = weddingId, Title = "Found Wedding" };
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var service = new WeddingService(repositoryMock.Object, new Mock<IUserContextService>().Object);

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
        var service = new WeddingService(repositoryMock.Object, new Mock<IUserContextService>().Object);

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
        var service = new WeddingService(repositoryMock.Object, new Mock<IUserContextService>().Object);

        var result = await service.GetByIdOrSlugAsync("slug-wedding");

        Assert.True(result.IsSuccess);
        Assert.Same(wedding, result.Value);
    }

    [Fact]
    public async Task GetByIdOrSlugAsync_ReturnsNotFoundWhenMissing()
    {
        var repositoryMock = new Mock<IWeddingRepository>();
        repositoryMock.Setup(r => r.GetByIdOrSlugAsync("ghost")).ReturnsAsync((Wedding?)null);
        var service = new WeddingService(repositoryMock.Object, new Mock<IUserContextService>().Object);

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
        var service = new WeddingService(repositoryMock.Object, new Mock<IUserContextService>().Object);

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
        var service = new WeddingService(repositoryMock.Object, new Mock<IUserContextService>().Object);

        var result = await service.DeleteAsync(weddingId);

        Assert.True(result.IsSuccess);
        repositoryMock.Verify(r => r.DeleteAsync(weddingId), Times.Once);
    }
}
