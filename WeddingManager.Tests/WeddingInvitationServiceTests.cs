using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class WeddingInvitationServiceTests
{
    private static WeddingInvitationService CreateService(
        Mock<IWeddingInvitationRepository>? invitationRepoMock = null,
        Mock<IWeddingUserRepository>? weddingUserRepoMock = null,
        Mock<IWeddingRepository>? weddingRepoMock = null,
        Mock<IEmailService>? emailServiceMock = null)
    {
        invitationRepoMock ??= new Mock<IWeddingInvitationRepository>();
        weddingUserRepoMock ??= new Mock<IWeddingUserRepository>();
        weddingRepoMock ??= new Mock<IWeddingRepository>();
        emailServiceMock ??= new Mock<IEmailService>();
        return new WeddingInvitationService(
            invitationRepoMock.Object, weddingUserRepoMock.Object,
            weddingRepoMock.Object, emailServiceMock.Object);
    }

    [Fact]
    public async Task CreateInvitationAsync_ReturnsValidationErrorForEmptyEmail()
    {
        var service = CreateService();
        var request = new CreateWeddingInvitationRequestDto { Email = "" };

        var result = await service.CreateInvitationAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateInvitationAsync_ReturnsConflictForExistingPending()
    {
        var weddingId = Guid.NewGuid();
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetPendingByEmailAsync(weddingId, "test@test.com"))
            .ReturnsAsync(new WeddingInvitation { Email = "test@test.com" });
        var service = CreateService(invitationRepoMock);
        var request = new CreateWeddingInvitationRequestDto { Email = "test@test.com" };

        var result = await service.CreateInvitationAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateInvitationAsync_CreatesInvitationWithPermissions()
    {
        var weddingId = Guid.NewGuid();
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetPendingByEmailAsync(weddingId, "planner@test.com"))
            .ReturnsAsync((WeddingInvitation?)null);
        WeddingInvitation? captured = null;
        invitationRepoMock.Setup(r => r.AddAsync(It.IsAny<WeddingInvitation>()))
            .Callback<WeddingInvitation>(wi => captured = wi).Returns(Task.CompletedTask);
        var service = CreateService(invitationRepoMock);
        var request = new CreateWeddingInvitationRequestDto
        {
            Email = "planner@test.com",
            CanAccessGuests = true,
            CanAccessEvents = false,
            CanAccessExpenses = true,
            CanAccessWebsite = false,
            IsReadOnly = true
        };

        var result = await service.CreateInvitationAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(weddingId, captured!.WeddingId);
        Assert.Equal("planner@test.com", captured.Email);
        Assert.Equal(WeddingUserRole.Planner, captured.Role);
        Assert.True(captured.CanAccessGuests);
        Assert.False(captured.CanAccessEvents);
        Assert.True(captured.CanAccessExpenses);
        Assert.False(captured.CanAccessWebsite);
        Assert.True(captured.IsReadOnly);
        Assert.NotEmpty(captured.Token);
        Assert.True(captured.ExpiresAt > DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task CancelInvitationAsync_ReturnsNotFoundWhenMissing()
    {
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((WeddingInvitation?)null);
        var service = CreateService(invitationRepoMock);

        var result = await service.CancelInvitationAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task CancelInvitationAsync_ReturnsValidationErrorWhenAlreadyAccepted()
    {
        var weddingId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByIdAsync(invitationId))
            .ReturnsAsync(new WeddingInvitation
            {
                Id = invitationId, WeddingId = weddingId, AcceptedAt = DateTime.UtcNow
            });
        var service = CreateService(invitationRepoMock);

        var result = await service.CancelInvitationAsync(weddingId, invitationId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task CancelInvitationAsync_DeletesWhenValid()
    {
        var weddingId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByIdAsync(invitationId))
            .ReturnsAsync(new WeddingInvitation
            {
                Id = invitationId, WeddingId = weddingId, AcceptedAt = null
            });
        invitationRepoMock.Setup(r => r.DeleteAsync(invitationId)).Returns(Task.CompletedTask);
        var service = CreateService(invitationRepoMock);

        var result = await service.CancelInvitationAsync(weddingId, invitationId);

        Assert.True(result.IsSuccess);
        invitationRepoMock.Verify(r => r.DeleteAsync(invitationId), Times.Once);
    }

    [Fact]
    public async Task GetByTokenAsync_ReturnsNotFoundWhenMissing()
    {
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByTokenAsync("token"))
            .ReturnsAsync((WeddingInvitation?)null);
        var service = CreateService(invitationRepoMock);

        var result = await service.GetByTokenAsync("token");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task GetByTokenAsync_ReturnsErrorWhenExpired()
    {
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByTokenAsync("expired"))
            .ReturnsAsync(new WeddingInvitation
            {
                Token = "expired", ExpiresAt = DateTime.UtcNow.AddDays(-1)
            });
        var service = CreateService(invitationRepoMock);

        var result = await service.GetByTokenAsync("expired");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task GetByTokenAsync_ReturnsErrorWhenAlreadyAccepted()
    {
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByTokenAsync("accepted"))
            .ReturnsAsync(new WeddingInvitation
            {
                Token = "accepted", ExpiresAt = DateTime.UtcNow.AddDays(10),
                AcceptedAt = DateTime.UtcNow
            });
        var service = CreateService(invitationRepoMock);

        var result = await service.GetByTokenAsync("accepted");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task GetByTokenAsync_ReturnsDtoWhenValid()
    {
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        var invitation = new WeddingInvitation
        {
            Id = Guid.NewGuid(), WeddingId = Guid.NewGuid(), Email = "planner@test.com",
            Token = "valid-token", ExpiresAt = DateTime.UtcNow.AddDays(10),
            AcceptedAt = null, Role = WeddingUserRole.Planner
        };
        invitationRepoMock.Setup(r => r.GetByTokenAsync("valid-token")).ReturnsAsync(invitation);
        var service = CreateService(invitationRepoMock);

        var result = await service.GetByTokenAsync("valid-token");

        Assert.True(result.IsSuccess);
        Assert.Equal(invitation.Id, result.Value!.Id);
        Assert.Equal("planner@test.com", result.Value.Email);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNotFoundWhenMissing()
    {
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByTokenAsync("missing"))
            .ReturnsAsync((WeddingInvitation?)null);
        var service = CreateService(invitationRepoMock);

        var result = await service.AcceptInvitationAsync("missing", Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsConflictWhenAlreadyMember()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByTokenAsync("token"))
            .ReturnsAsync(new WeddingInvitation
            {
                WeddingId = weddingId, Token = "token",
                ExpiresAt = DateTime.UtcNow.AddDays(10), AcceptedAt = null
            });
        var weddingUserRepoMock = new Mock<IWeddingUserRepository>();
        weddingUserRepoMock.Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync(new WeddingUser { WeddingId = weddingId, UserId = userId });
        var service = CreateService(invitationRepoMock, weddingUserRepoMock);

        var result = await service.AcceptInvitationAsync("token", userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task AcceptInvitationAsync_CreatesWeddingUserAndMarksAccepted()
    {
        var weddingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitation = new WeddingInvitation
        {
            Id = Guid.NewGuid(), WeddingId = weddingId, Token = "token",
            Email = "planner@test.com", Role = WeddingUserRole.Planner,
            ExpiresAt = DateTime.UtcNow.AddDays(10), AcceptedAt = null,
            CanAccessGuests = true, CanAccessEvents = false,
            CanAccessExpenses = true, CanAccessWebsite = false, IsReadOnly = true
        };
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByTokenAsync("token")).ReturnsAsync(invitation);
        invitationRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingInvitation>())).Returns(Task.CompletedTask);
        var weddingUserRepoMock = new Mock<IWeddingUserRepository>();
        weddingUserRepoMock.Setup(r => r.GetByIdAsync(weddingId, userId))
            .ReturnsAsync((WeddingUser?)null);
        WeddingUser? capturedUser = null;
        weddingUserRepoMock.Setup(r => r.AddAsync(It.IsAny<WeddingUser>()))
            .Callback<WeddingUser>(wu => capturedUser = wu).Returns(Task.CompletedTask);
        var service = CreateService(invitationRepoMock, weddingUserRepoMock);

        var result = await service.AcceptInvitationAsync("token", userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUser);
        Assert.Equal(weddingId, capturedUser!.WeddingId);
        Assert.Equal(userId, capturedUser.UserId);
        Assert.Equal(WeddingUserRole.Planner, capturedUser.Role);
        Assert.True(capturedUser.CanAccessGuests);
        Assert.False(capturedUser.CanAccessEvents);
        Assert.True(capturedUser.CanAccessExpenses);
        Assert.False(capturedUser.CanAccessWebsite);
        Assert.True(capturedUser.IsReadOnly);
        Assert.NotNull(invitation.AcceptedAt);
        invitationRepoMock.Verify(r => r.UpdateAsync(invitation), Times.Once);
    }

    [Fact]
    public async Task GetInvitationsAsync_MapsResults()
    {
        var weddingId = Guid.NewGuid();
        var invitationRepoMock = new Mock<IWeddingInvitationRepository>();
        invitationRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(new List<WeddingInvitation>
            {
                new() { Id = Guid.NewGuid(), WeddingId = weddingId, Email = "a@test.com", Role = WeddingUserRole.Planner },
                new() { Id = Guid.NewGuid(), WeddingId = weddingId, Email = "b@test.com", Role = WeddingUserRole.Planner }
            });
        var service = CreateService(invitationRepoMock);

        var result = await service.GetInvitationsAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
    }
}
