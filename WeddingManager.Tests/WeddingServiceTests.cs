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
}
