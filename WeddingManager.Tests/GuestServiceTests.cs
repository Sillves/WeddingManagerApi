using Microsoft.Extensions.Logging;
using Moq;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class GuestServiceTests
{
    private readonly ApplicationMapper _mapper = new();

    private GuestService CreateService(
        Mock<IGuestRepository>? guestRepositoryMock = null,
        Mock<IWeddingRepository>? weddingRepositoryMock = null,
        Mock<IEmailService>? emailServiceMock = null,
        Mock<ISubscriptionLimitService>? limitServiceMock = null)
    {
        guestRepositoryMock ??= new Mock<IGuestRepository>();
        weddingRepositoryMock ??= new Mock<IWeddingRepository>();
        emailServiceMock ??= new Mock<IEmailService>();
        limitServiceMock ??= new Mock<ISubscriptionLimitService>();
        return new GuestService(
            guestRepositoryMock.Object,
            weddingRepositoryMock.Object,
            emailServiceMock.Object,
            limitServiceMock.Object,
            _mapper,
            Mock.Of<ILogger<GuestService>>());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsGuestWhenFound()
    {
        var guestId = Guid.NewGuid();
        var guest = new Guest { Id = guestId, Name = "Jamie", Email = "jamie@example.com", WeddingId = Guid.NewGuid() };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync(guest);
        var service = CreateService(guestRepositoryMock: repoMock);

        var result = await service.GetByIdAsync(guestId);

        Assert.True(result.IsSuccess);
        Assert.Equal(guest.Name, result.Value!.Name);
        Assert.Equal(guest.Email, result.Value.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundWhenMissing()
    {
        var guestId = Guid.NewGuid();
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync((Guest?)null);
        var service = CreateService(guestRepositoryMock: repoMock);

        var result = await service.GetByIdAsync(guestId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task GetByWeddingIdAsync_ReturnsList()
    {
        var weddingId = Guid.NewGuid();
        var guests = new List<Guest>
        {
            new() { Id = Guid.NewGuid(), Name = "Jamie", Email = "jamie@example.com", WeddingId = weddingId },
            new() { Id = Guid.NewGuid(), Name = "Alex", Email = "alex@example.com", WeddingId = weddingId }
        };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(guests);
        var service = CreateService(guestRepositoryMock: repoMock);

        var result = await service.GetByWeddingIdAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task GetByWeddingIdAsync_ReturnsEmptyList()
    {
        var weddingId = Guid.NewGuid();
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(new List<Guest>());
        var service = CreateService(guestRepositoryMock: repoMock);

        var result = await service.GetByWeddingIdAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task CreateGuestAsync_ReturnsValidationErrorForInvalidInput()
    {
        var weddingId = Guid.NewGuid();
        var service = CreateService();
        var request = new CreateGuestRequestDto { Name = "", Email = "invalid", PreferredLanguage = "en" };

        var result = await service.CreateGuestAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation);
    }

    [Fact]
    public async Task CreateGuestAsync_ReturnsErrorWhenLimitExceeded()
    {
        var weddingId = Guid.NewGuid();
        var limitMock = new Mock<ISubscriptionLimitService>();
        limitMock.Setup(l => l.EnsureGuestLimitAsync(weddingId))
            .ReturnsAsync(Result.Fail(new Error(ErrorCodes.LimitExceeded, "Guest limit reached")));
        var service = CreateService(limitServiceMock: limitMock);
        var request = new CreateGuestRequestDto { Name = "Jamie", Email = "jamie@example.com", PreferredLanguage = "en" };

        var result = await service.CreateGuestAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.LimitExceeded, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateGuestAsync_ReturnsConflictForDuplicateEmail()
    {
        var weddingId = Guid.NewGuid();
        var existingGuest = new Guest { Id = Guid.NewGuid(), Name = "Jamie", Email = "jamie@example.com", WeddingId = weddingId };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByEmailAsync(weddingId, "jamie@example.com")).ReturnsAsync(existingGuest);
        var limitMock = new Mock<ISubscriptionLimitService>();
        limitMock.Setup(l => l.EnsureGuestLimitAsync(weddingId)).ReturnsAsync(Result.Ok());
        var service = CreateService(guestRepositoryMock: repoMock, limitServiceMock: limitMock);
        var request = new CreateGuestRequestDto { Name = "Jamie Dup", Email = "jamie@example.com", PreferredLanguage = "en" };

        var result = await service.CreateGuestAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task UpdateGuestAsync_ReturnsUpdatedGuest()
    {
        var guestId = Guid.NewGuid();
        var weddingId = Guid.NewGuid();
        var guest = new Guest { Id = guestId, Name = "Old Name", Email = "old@example.com", WeddingId = weddingId };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync(guest);
        repoMock.Setup(r => r.GetByEmailAsync(weddingId, "new@example.com")).ReturnsAsync((Guest?)null);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Guest>())).Returns(Task.CompletedTask);
        var service = CreateService(guestRepositoryMock: repoMock);
        var request = new UpdateGuestRequestDto { Name = "New Name", Email = "new@example.com", PreferredLanguage = "en" };

        var result = await service.UpdateGuestAsync(guestId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal("new@example.com", result.Value.Email);
    }

    [Fact]
    public async Task UpdateGuestAsync_ReturnsNotFoundWhenMissing()
    {
        var guestId = Guid.NewGuid();
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync((Guest?)null);
        var service = CreateService(guestRepositoryMock: repoMock);
        var request = new UpdateGuestRequestDto { Name = "Name", Email = "email@example.com", PreferredLanguage = "en" };

        var result = await service.UpdateGuestAsync(guestId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task UpdateGuestAsync_ReturnsValidationErrorForInvalidInput()
    {
        var guestId = Guid.NewGuid();
        var guest = new Guest { Id = guestId, Name = "Name", Email = "email@example.com", WeddingId = Guid.NewGuid() };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync(guest);
        var service = CreateService(guestRepositoryMock: repoMock);
        var request = new UpdateGuestRequestDto { Name = "", Email = "invalid", PreferredLanguage = "en" };

        var result = await service.UpdateGuestAsync(guestId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation);
    }

    [Fact]
    public async Task UpdateGuestAsync_ReturnsConflictForDuplicateEmail()
    {
        var guestId = Guid.NewGuid();
        var weddingId = Guid.NewGuid();
        var guest = new Guest { Id = guestId, Name = "Jamie", Email = "old@example.com", WeddingId = weddingId };
        var existingGuest = new Guest { Id = Guid.NewGuid(), Name = "Other", Email = "taken@example.com", WeddingId = weddingId };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync(guest);
        repoMock.Setup(r => r.GetByEmailAsync(weddingId, "taken@example.com")).ReturnsAsync(existingGuest);
        var service = CreateService(guestRepositoryMock: repoMock);
        var request = new UpdateGuestRequestDto { Name = "Jamie", Email = "taken@example.com", PreferredLanguage = "en" };

        var result = await service.UpdateGuestAsync(guestId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task DeleteGuestAsync_ReturnsOkWhenFound()
    {
        var guestId = Guid.NewGuid();
        var guest = new Guest { Id = guestId, Name = "Jamie", Email = "jamie@example.com", WeddingId = Guid.NewGuid() };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync(guest);
        repoMock.Setup(r => r.DeleteAsync(guestId)).Returns(Task.CompletedTask);
        var service = CreateService(guestRepositoryMock: repoMock);

        var result = await service.DeleteGuestAsync(guestId);

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.DeleteAsync(guestId), Times.Once);
    }

    [Fact]
    public async Task DeleteGuestAsync_ReturnsNotFoundWhenMissing()
    {
        var guestId = Guid.NewGuid();
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync((Guest?)null);
        var service = CreateService(guestRepositoryMock: repoMock);

        var result = await service.DeleteGuestAsync(guestId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task SubmitRsvpAsync_ReturnsUpdatedGuest()
    {
        var weddingId = Guid.NewGuid();
        var guest = new Guest { Id = Guid.NewGuid(), Name = "Jamie", Email = "jamie@example.com", WeddingId = weddingId, RsvpStatus = RsvpStatus.Pending };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByEmailAsync(weddingId, "jamie@example.com")).ReturnsAsync(guest);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Guest>())).Returns(Task.CompletedTask);
        var service = CreateService(guestRepositoryMock: repoMock);
        var request = new RsvpSubmitRequestDto { Name = "Jamie Updated", Email = "jamie@example.com", RsvpStatus = RsvpStatus.Accepted };

        var result = await service.SubmitRsvpAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(RsvpStatus.Accepted, result.Value!.RsvpStatus);
        Assert.Equal("Jamie Updated", result.Value.Name);
    }

    [Fact]
    public async Task SubmitRsvpAsync_ReturnsNotFoundWhenGuestMissing()
    {
        var weddingId = Guid.NewGuid();
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetByEmailAsync(weddingId, "ghost@example.com")).ReturnsAsync((Guest?)null);
        var service = CreateService(guestRepositoryMock: repoMock);
        var request = new RsvpSubmitRequestDto { Name = "Ghost", Email = "ghost@example.com", RsvpStatus = RsvpStatus.Accepted };

        var result = await service.SubmitRsvpAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task SubmitRsvpAsync_ReturnsValidationErrorForInvalidInput()
    {
        var weddingId = Guid.NewGuid();
        var service = CreateService();
        var request = new RsvpSubmitRequestDto { Name = "", Email = "" };

        var result = await service.SubmitRsvpAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation);
    }

    [Fact]
    public async Task CheckExistingEmailsAsync_ReturnsMatchingEmails()
    {
        var weddingId = Guid.NewGuid();
        var emails = new[] { "jamie@example.com", "alex@example.com", "new@example.com" };
        var existing = new[] { "jamie@example.com", "alex@example.com" };
        var repoMock = new Mock<IGuestRepository>();
        repoMock.Setup(r => r.GetExistingEmailsAsync(weddingId, emails)).ReturnsAsync(existing);
        var service = CreateService(guestRepositoryMock: repoMock);

        var result = await service.CheckExistingEmailsAsync(weddingId, emails);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task CreateGuestAsync_EnforcesLimitAndAddsGuest()
    {
        var weddingId = Guid.NewGuid();
        var repositoryMock = new Mock<IGuestRepository>();
        repositoryMock.Setup(r => r.GetByEmailAsync(weddingId, It.IsAny<string>()))
            .ReturnsAsync((Guest?)null);
        repositoryMock.Setup(r => r.AddAsync(It.IsAny<Guest>()))
            .Returns(Task.CompletedTask);

        var limitService = new Mock<ISubscriptionLimitService>();
        limitService.Setup(l => l.EnsureGuestLimitAsync(weddingId)).ReturnsAsync(Result.Ok());

        var service = CreateService(guestRepositoryMock: repositoryMock, limitServiceMock: limitService);

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
        var wedding = new Wedding { Id = weddingId, UserId = userId, User = new User { Id = userId } };
        var guestId = Guid.NewGuid();
        var guest = new Guest
        {
            Id = guestId, WeddingId = weddingId, Name = "Jamie", Email = "jamie@example.com",
            InvitationToken = "token", InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1)
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

        var service = CreateService(repositoryMock, weddingRepository, emailService, limitService);

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
        var wedding = new Wedding { Id = weddingId, UserId = userId, User = new User { Id = userId } };
        var guests = new List<Guest>
        {
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Jamie", Email = "jamie@example.com", RsvpStatus = RsvpStatus.Pending, InvitationToken = "token-1", InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Alex", Email = "alex@example.com", RsvpStatus = RsvpStatus.Pending, InvitationToken = "token-2", InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1) }
        };

        var repositoryMock = new Mock<IGuestRepository>();
        repositoryMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(guests);
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Guest>())).Returns(Task.CompletedTask);
        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendInvitationAsync(It.IsAny<Guest>(), wedding)).Returns(Task.CompletedTask);
        var limitService = new Mock<ISubscriptionLimitService>();
        limitService.Setup(l => l.EnsureEmailLimitAsync(weddingId, guests.Count)).ReturnsAsync(Result.Ok());
        limitService.Setup(l => l.RecordEmailsSentAsync(userId, guests.Count)).ReturnsAsync(Result.Ok());

        var service = CreateService(repositoryMock, weddingRepository, emailService, limitService);

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
        var wedding = new Wedding { Id = weddingId, UserId = userId, User = new User { Id = userId } };
        var guests = new List<Guest>
        {
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Jamie", Email = "jamie@example.com", RsvpStatus = RsvpStatus.Pending, InvitationToken = "token-1", InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Alex", Email = "alex@example.com", RsvpStatus = RsvpStatus.Pending, InvitationToken = "token-2", InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(1) }
        };

        var repositoryMock = new Mock<IGuestRepository>();
        repositoryMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(guests);
        repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Guest>())).Returns(Task.CompletedTask);
        var weddingRepository = new Mock<IWeddingRepository>();
        weddingRepository.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendInvitationAsync(guests[0], wedding)).Returns(Task.CompletedTask);
        emailService.Setup(e => e.SendInvitationAsync(guests[1], wedding)).ThrowsAsync(new InvalidOperationException("Boom"));
        var limitService = new Mock<ISubscriptionLimitService>();
        limitService.Setup(l => l.EnsureEmailLimitAsync(weddingId, guests.Count)).ReturnsAsync(Result.Ok());
        limitService.Setup(l => l.RecordEmailsSentAsync(userId, 1)).ReturnsAsync(Result.Ok());

        var service = CreateService(repositoryMock, weddingRepository, emailService, limitService);

        var result = await service.SendInvitationsAsync(weddingId, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.SentCount);
        Assert.Equal(1, result.Value!.FailedCount);
        limitService.Verify(l => l.RecordEmailsSentAsync(userId, 1), Times.Once);
    }
}
