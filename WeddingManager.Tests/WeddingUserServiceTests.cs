using Moq;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class WeddingUserServiceTests
{
    private readonly ApplicationMapper _mapper = new();

    [Fact]
    public async Task AddUserToWeddingAsync_ThrowsWhenAlreadyAdded()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync(new WeddingUser { WeddingId = weddingId, UserId = userId });
        var service = new WeddingUserService(repositoryMock.Object, _mapper);
        var request = new AddWeddingUserRequestDto { UserId = userId, Role = WeddingUserRole.Owner };

        var result = await service.AddUserToWeddingAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<WeddingUser>()), Times.Never);
    }

    [Fact]
    public async Task AddUserToWeddingAsync_AddsUserAndMapsResult()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync((WeddingUser?)null);
        WeddingUser? captured = null;
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<WeddingUser>()))
            .Callback<WeddingUser>(w => captured = w)
            .Returns(Task.CompletedTask);
        var service = new WeddingUserService(repositoryMock.Object, _mapper);
        var request = new AddWeddingUserRequestDto { UserId = userId, Role = WeddingUserRole.Planner };

        var result = await service.AddUserToWeddingAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(weddingId, captured!.WeddingId);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal(request.Role, captured.Role);
        Assert.True(DateTime.UtcNow - captured.AddedAt < TimeSpan.FromSeconds(5));
        Assert.Equal(weddingId, result.Value!.WeddingId);
        Assert.Equal(userId, result.Value!.UserId);
        Assert.Equal(request.Role, result.Value!.Role);
        repositoryMock.Verify(r => r.AddAsync(It.IsAny<WeddingUser>()), Times.Once);
    }

    [Fact]
    public async Task GetWeddingUsersAsync_MapsResults()
    {
        var weddingId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        var users = new List<WeddingUser>
        {
            new()
            {
                WeddingId = weddingId,
                UserId = Guid.NewGuid(),
                Role = WeddingUserRole.Owner,
                User = new User { FirstName = "Test", LastName = "User", Email = "test@example.com" }
            }
        };
        repositoryMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(users);
        var service = new WeddingUserService(repositoryMock.Object, _mapper);

        var result = await service.GetWeddingUsersAsync(weddingId);

        Assert.True(result.IsSuccess);
        var list = result.Value!.ToList();
        Assert.Single(list);
        Assert.Equal(weddingId, list[0].WeddingId);
    }

    [Fact]
    public async Task RemoveUserFromWeddingAsync_ThrowsWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync((WeddingUser?)null);
        var service = new WeddingUserService(repositoryMock.Object, _mapper);

        var result = await service.RemoveUserFromWeddingAsync(weddingId, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
        repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RemoveUserFromWeddingAsync_DeletesWhenFound()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync(new WeddingUser { WeddingId = weddingId, UserId = userId, Role = WeddingUserRole.Planner });
        repositoryMock
            .Setup(r => r.DeleteAsync(weddingId, userId))
            .Returns(Task.CompletedTask);
        var service = new WeddingUserService(repositoryMock.Object, _mapper);

        var result = await service.RemoveUserFromWeddingAsync(weddingId, userId);

        Assert.True(result.IsSuccess);
        repositoryMock.Verify(r => r.DeleteAsync(weddingId, userId), Times.Once);
    }

    [Fact]
    public async Task AddUserToWeddingAsync_CopiesPermissionsFromRequest()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId, userId)).ReturnsAsync((WeddingUser?)null);
        WeddingUser? captured = null;
        repositoryMock.Setup(r => r.AddAsync(It.IsAny<WeddingUser>()))
            .Callback<WeddingUser>(w => captured = w).Returns(Task.CompletedTask);
        var service = new WeddingUserService(repositoryMock.Object, _mapper);
        var request = new AddWeddingUserRequestDto
        {
            UserId = userId,
            Role = WeddingUserRole.Planner,
            CanAccessGuests = true,
            CanAccessEvents = false,
            CanAccessExpenses = true,
            CanAccessWebsite = false,
            IsReadOnly = true
        };

        var result = await service.AddUserToWeddingAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.True(captured!.CanAccessGuests);
        Assert.False(captured.CanAccessEvents);
        Assert.True(captured.CanAccessExpenses);
        Assert.False(captured.CanAccessWebsite);
        Assert.True(captured.IsReadOnly);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_ReturnsNotFoundWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId, userId)).ReturnsAsync((WeddingUser?)null);
        var service = new WeddingUserService(repositoryMock.Object, _mapper);
        var request = new UpdateWeddingUserRequestDto
        {
            CanAccessGuests = true, CanAccessEvents = true,
            CanAccessExpenses = true, CanAccessWebsite = true, IsReadOnly = false
        };

        var result = await service.UpdatePermissionsAsync(weddingId, userId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_UpdatesAndReturnsDto()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        var existingUser = new WeddingUser
        {
            WeddingId = weddingId, UserId = userId, Role = WeddingUserRole.Planner,
            CanAccessGuests = true, CanAccessEvents = true, CanAccessExpenses = true,
            CanAccessWebsite = false, IsReadOnly = false,
            User = new User { FirstName = "Test", LastName = "Planner", Email = "planner@test.com" }
        };
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId, userId)).ReturnsAsync(existingUser);
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingUser>())).Returns(Task.CompletedTask);
        var service = new WeddingUserService(repositoryMock.Object, _mapper);
        var request = new UpdateWeddingUserRequestDto
        {
            CanAccessGuests = false, CanAccessEvents = true,
            CanAccessExpenses = false, CanAccessWebsite = true, IsReadOnly = true
        };

        var result = await service.UpdatePermissionsAsync(weddingId, userId, request);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanAccessGuests);
        Assert.True(result.Value!.CanAccessEvents);
        Assert.False(result.Value!.CanAccessExpenses);
        Assert.True(result.Value!.CanAccessWebsite);
        Assert.True(result.Value!.IsReadOnly);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WeddingUser>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_ReturnsForbiddenForOwner()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync(new WeddingUser
            {
                WeddingId = weddingId, UserId = userId, Role = WeddingUserRole.Owner,
                User = new User { FirstName = "Owner", LastName = "User", Email = "owner@test.com" }
            });
        var service = new WeddingUserService(repositoryMock.Object, _mapper);
        var request = new UpdateWeddingUserRequestDto
        {
            CanAccessGuests = false, CanAccessEvents = false,
            CanAccessExpenses = false, CanAccessWebsite = false, IsReadOnly = true
        };

        var result = await service.UpdatePermissionsAsync(weddingId, userId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Forbidden, result.Errors[0].Code);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WeddingUser>()), Times.Never);
    }

    [Fact]
    public async Task RemoveUserFromWeddingAsync_ReturnsForbiddenForOwner()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repositoryMock = new Mock<IWeddingUserRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync(new WeddingUser { WeddingId = weddingId, UserId = userId, Role = WeddingUserRole.Owner });
        var service = new WeddingUserService(repositoryMock.Object, _mapper);

        var result = await service.RemoveUserFromWeddingAsync(weddingId, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Forbidden, result.Errors[0].Code);
        repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }
}
