using System.Text.Json;
using Moq;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class WeddingWebsiteServiceTests
{
    private readonly ApplicationMapper _mapper = new();

    private WeddingWebsiteService CreateService(
        Mock<IWeddingWebsiteRepository>? websiteRepoMock = null,
        Mock<IWeddingRepository>? weddingRepoMock = null,
        Mock<IEventRepository>? eventRepoMock = null,
        Mock<IInvitationFlowRepository>? flowRepoMock = null)
    {
        websiteRepoMock ??= new Mock<IWeddingWebsiteRepository>();
        weddingRepoMock ??= new Mock<IWeddingRepository>();
        eventRepoMock ??= new Mock<IEventRepository>();
        if (flowRepoMock == null)
        {
            flowRepoMock = new Mock<IInvitationFlowRepository>();
            flowRepoMock.Setup(r => r.GetByWeddingIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InvitationFlow>());
        }
        return new WeddingWebsiteService(
            websiteRepoMock.Object,
            weddingRepoMock.Object,
            eventRepoMock.Object,
            flowRepoMock.Object,
            _mapper);
    }

    private static WeddingWebsite CreateWebsiteEntity(
        Guid? weddingId = null,
        bool isPublished = false,
        string content = "{}",
        string settings = "{}")
    {
        var wId = weddingId ?? Guid.NewGuid();
        return new WeddingWebsite
        {
            Id = Guid.NewGuid(),
            WeddingId = wId,
            Wedding = new Wedding { Id = wId, Title = "Test Wedding", Slug = "test-wedding", Date = DateTime.UtcNow.AddMonths(3), Location = "Amsterdam" },
            Template = WebsiteTemplate.ElegantClassic,
            Content = content,
            Settings = settings,
            IsPublished = isPublished,
            PublishedAt = isPublished ? DateTime.UtcNow : null,
            MetaDescription = "A test wedding website",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // --- GetByWeddingIdAsync ---

    [Fact]
    public async Task GetByWeddingIdAsync_ReturnsWebsiteWhenFound()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId);
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(website);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.GetByWeddingIdAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.Equal(website.Id, result.Value!.Id);
        Assert.Equal(website.Template, result.Value.Template);
    }

    [Fact]
    public async Task GetByWeddingIdAsync_ReturnsNotFoundWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync((WeddingWebsite?)null);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.GetByWeddingIdAsync(weddingId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ReturnsNotFoundWhenWeddingMissing()
    {
        var weddingId = Guid.NewGuid();
        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync((Wedding?)null);
        var service = CreateService(weddingRepoMock: weddingRepoMock);

        var result = await service.CreateAsync(Guid.NewGuid(), weddingId, new CreateWeddingWebsiteRequestDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationWhenNoWeddingDate()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId, Title = "No Date Wedding", Date = default,
            Location = "Somewhere", User = new User { SubscriptionTier = SubscriptionTier.Starter }
        };
        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var service = CreateService(weddingRepoMock: weddingRepoMock);

        var result = await service.CreateAsync(Guid.NewGuid(), weddingId, new CreateWeddingWebsiteRequestDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
        Assert.Contains("wedding date", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ReturnsForbiddenForFreeTier()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId, Title = "Free Wedding", Date = DateTime.UtcNow.AddMonths(3),
            Location = "Here", User = new User { SubscriptionTier = SubscriptionTier.Free }
        };
        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var service = CreateService(weddingRepoMock: weddingRepoMock);

        var result = await service.CreateAsync(Guid.NewGuid(), weddingId, new CreateWeddingWebsiteRequestDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Forbidden, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflictWhenAlreadyExists()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId, Title = "Existing", Date = DateTime.UtcNow.AddMonths(3),
            Location = "Here", User = new User { SubscriptionTier = SubscriptionTier.Starter }
        };
        var existingWebsite = CreateWebsiteEntity(weddingId);
        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(existingWebsite);
        var service = CreateService(websiteRepoMock, weddingRepoMock);

        var result = await service.CreateAsync(Guid.NewGuid(), weddingId, new CreateWeddingWebsiteRequestDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateAsync_CreatesWebsiteForStarterTier()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId, Title = "Starter Wedding", Slug = "starter-wedding",
            Date = DateTime.UtcNow.AddMonths(3), Location = "Amsterdam",
            User = new User { SubscriptionTier = SubscriptionTier.Starter }
        };
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        // First call (check existing) returns null, second call (reload) returns created
        var createdWebsite = CreateWebsiteEntity(weddingId);
        websiteRepoMock.SetupSequence(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync((WeddingWebsite?)null)
            .ReturnsAsync(createdWebsite);
        websiteRepoMock.Setup(r => r.AddAsync(It.IsAny<WeddingWebsite>())).Returns(Task.CompletedTask);
        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var service = CreateService(websiteRepoMock, weddingRepoMock);

        var result = await service.CreateAsync(Guid.NewGuid(), weddingId, new CreateWeddingWebsiteRequestDto
        {
            Template = WebsiteTemplate.ElegantClassic
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        websiteRepoMock.Verify(r => r.AddAsync(It.Is<WeddingWebsite>(w =>
            w.WeddingId == weddingId && w.Template == WebsiteTemplate.ElegantClassic)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CreatesWebsiteForProTier()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding
        {
            Id = weddingId, Title = "Pro Wedding", Slug = "pro-wedding",
            Date = DateTime.UtcNow.AddMonths(6), Location = "Paris",
            User = new User { SubscriptionTier = SubscriptionTier.Pro }
        };
        var createdWebsite = CreateWebsiteEntity(weddingId);
        createdWebsite.Template = WebsiteTemplate.ModernMinimal;
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.SetupSequence(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync((WeddingWebsite?)null)
            .ReturnsAsync(createdWebsite);
        websiteRepoMock.Setup(r => r.AddAsync(It.IsAny<WeddingWebsite>())).Returns(Task.CompletedTask);
        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId)).ReturnsAsync(wedding);
        var service = CreateService(websiteRepoMock, weddingRepoMock);

        var result = await service.CreateAsync(Guid.NewGuid(), weddingId, new CreateWeddingWebsiteRequestDto
        {
            Template = WebsiteTemplate.ModernMinimal
        });

        Assert.True(result.IsSuccess);
        websiteRepoMock.Verify(r => r.AddAsync(It.Is<WeddingWebsite>(w =>
            w.Template == WebsiteTemplate.ModernMinimal)), Times.Once);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ReturnsNotFoundWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync((WeddingWebsite?)null);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.UpdateAsync(weddingId, new UpdateWeddingWebsiteRequestDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTemplate()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId);
        var updatedWebsite = CreateWebsiteEntity(weddingId);
        updatedWebsite.Template = WebsiteTemplate.RomanticGarden;
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.SetupSequence(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(website)
            .ReturnsAsync(updatedWebsite);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingWebsite>())).Returns(Task.CompletedTask);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.UpdateAsync(weddingId, new UpdateWeddingWebsiteRequestDto
        {
            Template = WebsiteTemplate.RomanticGarden
        });

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.UpdateAsync(It.Is<WeddingWebsite>(w =>
            w.Template == WebsiteTemplate.RomanticGarden)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSettingsAndContent()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId);
        var updatedWebsite = CreateWebsiteEntity(weddingId);
        updatedWebsite.Settings = """{"primaryColor":"#FF0000"}""";
        updatedWebsite.Content = """{"hero":{"coupleNames":"Updated"}}""";
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.SetupSequence(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(website)
            .ReturnsAsync(updatedWebsite);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingWebsite>())).Returns(Task.CompletedTask);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.UpdateAsync(weddingId, new UpdateWeddingWebsiteRequestDto
        {
            Settings = JsonDocument.Parse("""{"primaryColor":"#FF0000"}"""),
            Content = JsonDocument.Parse("""{"hero":{"coupleNames":"Updated"}}""")
        });

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<WeddingWebsite>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMetaDescription()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId);
        var updatedWebsite = CreateWebsiteEntity(weddingId);
        updatedWebsite.MetaDescription = "Updated description";
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.SetupSequence(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(website)
            .ReturnsAsync(updatedWebsite);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingWebsite>())).Returns(Task.CompletedTask);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.UpdateAsync(weddingId, new UpdateWeddingWebsiteRequestDto
        {
            MetaDescription = "Updated description"
        });

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.UpdateAsync(It.Is<WeddingWebsite>(w =>
            w.MetaDescription == "Updated description")), Times.Once);
    }

    // --- PublishAsync ---

    [Fact]
    public async Task PublishAsync_PublishesWebsite()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId, isPublished: false);
        var publishedWebsite = CreateWebsiteEntity(weddingId, isPublished: true);
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.SetupSequence(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(website)
            .ReturnsAsync(publishedWebsite);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingWebsite>())).Returns(Task.CompletedTask);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.PublishAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublished);
        repoMock.Verify(r => r.UpdateAsync(It.Is<WeddingWebsite>(w =>
            w.IsPublished && w.PublishedAt != null)), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ReturnsNotFoundWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync((WeddingWebsite?)null);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.PublishAsync(weddingId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    // --- UnpublishAsync ---

    [Fact]
    public async Task UnpublishAsync_UnpublishesWebsite()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId, isPublished: true);
        var unpublishedWebsite = CreateWebsiteEntity(weddingId, isPublished: false);
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.SetupSequence(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(website)
            .ReturnsAsync(unpublishedWebsite);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingWebsite>())).Returns(Task.CompletedTask);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.UnpublishAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsPublished);
        repoMock.Verify(r => r.UpdateAsync(It.Is<WeddingWebsite>(w => !w.IsPublished)), Times.Once);
    }

    [Fact]
    public async Task UnpublishAsync_ReturnsNotFoundWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync((WeddingWebsite?)null);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.UnpublishAsync(weddingId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    // --- GetPublicBySlugAsync ---

    [Fact]
    public async Task GetPublicBySlugAsync_ReturnsPublicDto()
    {
        var website = CreateWebsiteEntity(isPublished: true);
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", null);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.RequiresPasscode);
        Assert.Equal("test-wedding", result.Value.Website!.WeddingSlug);
        Assert.Equal("Test Wedding", result.Value.Website.CoupleNames);
        Assert.Equal(website.Wedding.Location, result.Value.Website.WeddingLocation);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_ReturnsNotFoundWhenMissing()
    {
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetPublishedBySlugAsync("ghost")).ReturnsAsync((WeddingWebsite?)null);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.GetPublicBySlugAsync("ghost", null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_IncludesEventsWhenEnabled()
    {
        var weddingId = Guid.NewGuid();
        var content = """{"events":{"enabled":true,"showFromWeddingEvents":true}}""";
        var website = CreateWebsiteEntity(weddingId, isPublished: true, content: content);
        var events = new List<Event>
        {
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Ceremony", Location = "Church", StartDate = DateTime.UtcNow, Guests = new List<Guest>() },
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Reception", Location = "Ballroom", StartDate = DateTime.UtcNow, Guests = new List<Guest>() }
        };
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var eventRepoMock = new Mock<IEventRepository>();
        eventRepoMock.Setup(r => r.GetByWeddingIdForPublicAsync(weddingId)).ReturnsAsync(events);
        var service = CreateService(websiteRepoMock, eventRepoMock: eventRepoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", null);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Website!.Events);
        Assert.Equal(2, result.Value.Website.Events!.Count);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_NeverExposesGuestList()
    {
        var weddingId = Guid.NewGuid();
        var content = """{"events":{"enabled":true,"showFromWeddingEvents":true}}""";
        var website = CreateWebsiteEntity(weddingId, isPublished: true, content: content);
        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Ceremony", Location = "Church", StartDate = DateTime.UtcNow,
                Guests = new List<Guest>
                {
                    new() { Id = Guid.NewGuid(), Name = "Jane", Surname = "Doe", Email = "jane@x.com" }
                }
            }
        };
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var eventRepoMock = new Mock<IEventRepository>();
        eventRepoMock.Setup(r => r.GetByWeddingIdForPublicAsync(weddingId)).ReturnsAsync(events);
        var service = CreateService(websiteRepoMock, eventRepoMock: eventRepoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", null);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Website!.Events);
        Assert.All(result.Value.Website.Events!, e => Assert.Empty(e.GuestDtos));
    }

    [Fact]
    public async Task GetPublicBySlugAsync_ExcludesEventsWhenDisabled()
    {
        var content = """{"events":{"enabled":false,"showFromWeddingEvents":false}}""";
        var website = CreateWebsiteEntity(isPublished: true, content: content);
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var eventRepoMock = new Mock<IEventRepository>();
        var service = CreateService(websiteRepoMock, eventRepoMock: eventRepoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Website!.Events);
        eventRepoMock.Verify(r => r.GetByWeddingIdForPublicAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_RequiresPasscode_WhenPasscodeFlowsAndNoUnlock()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId, isPublished: true);
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var flowRepoMock = new Mock<IInvitationFlowRepository>();
        flowRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(new List<InvitationFlow>
        {
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Family", Passcode = "secret" }
        });
        var service = CreateService(websiteRepoMock, flowRepoMock: flowRepoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", null);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.RequiresPasscode);
        Assert.Null(result.Value.Website);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_RequiresPasscode_WhenUnlockedFlowNotOfThisWedding()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId, isPublished: true);
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var flowRepoMock = new Mock<IInvitationFlowRepository>();
        flowRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(new List<InvitationFlow>
        {
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Family", Passcode = "secret" }
        });
        var service = CreateService(websiteRepoMock, flowRepoMock: flowRepoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.RequiresPasscode);
        Assert.Null(result.Value.Website);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_FiltersEventsToUnlockedFlow()
    {
        var weddingId = Guid.NewGuid();
        var content = """{"events":{"enabled":true,"showFromWeddingEvents":true}}""";
        var website = CreateWebsiteEntity(weddingId, isPublished: true, content: content);
        var ceremonyId = Guid.NewGuid();
        var receptionId = Guid.NewGuid();
        var events = new List<Event>
        {
            new() { Id = ceremonyId, WeddingId = weddingId, Name = "Ceremony", Location = "Church", StartDate = DateTime.UtcNow, Guests = new List<Guest>() },
            new() { Id = receptionId, WeddingId = weddingId, Name = "Reception", Location = "Ballroom", StartDate = DateTime.UtcNow, Guests = new List<Guest>() }
        };
        var flowId = Guid.NewGuid();
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var eventRepoMock = new Mock<IEventRepository>();
        eventRepoMock.Setup(r => r.GetByWeddingIdForPublicAsync(weddingId)).ReturnsAsync(events);
        var flowRepoMock = new Mock<IInvitationFlowRepository>();
        flowRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(new List<InvitationFlow>
        {
            new() { Id = flowId, WeddingId = weddingId, Name = "Ceremony only", Passcode = "secret", EventIds = new List<Guid> { ceremonyId } }
        });
        var service = CreateService(websiteRepoMock, eventRepoMock: eventRepoMock, flowRepoMock: flowRepoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", flowId);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.RequiresPasscode);
        Assert.NotNull(result.Value.Website!.Events);
        Assert.Single(result.Value.Website.Events!);
        Assert.Equal("Ceremony", result.Value.Website.Events![0].Name);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_OpenFlow_FiltersEventsWithoutPasscode()
    {
        var weddingId = Guid.NewGuid();
        var content = """{"events":{"enabled":true,"showFromWeddingEvents":true}}""";
        var website = CreateWebsiteEntity(weddingId, isPublished: true, content: content);
        var ceremonyId = Guid.NewGuid();
        var receptionId = Guid.NewGuid();
        var events = new List<Event>
        {
            new() { Id = ceremonyId, WeddingId = weddingId, Name = "Ceremony", Location = "Church", StartDate = DateTime.UtcNow, Guests = new List<Guest>() },
            new() { Id = receptionId, WeddingId = weddingId, Name = "Reception", Location = "Ballroom", StartDate = DateTime.UtcNow, Guests = new List<Guest>() }
        };
        var websiteRepoMock = new Mock<IWeddingWebsiteRepository>();
        websiteRepoMock.Setup(r => r.GetPublishedBySlugAsync("test-wedding")).ReturnsAsync(website);
        var eventRepoMock = new Mock<IEventRepository>();
        eventRepoMock.Setup(r => r.GetByWeddingIdForPublicAsync(weddingId)).ReturnsAsync(events);
        var flowRepoMock = new Mock<IInvitationFlowRepository>();
        flowRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(new List<InvitationFlow>
        {
            new() { Id = Guid.NewGuid(), WeddingId = weddingId, Name = "Everyone", Passcode = null, EventIds = new List<Guid> { receptionId } }
        });
        var service = CreateService(websiteRepoMock, eventRepoMock: eventRepoMock, flowRepoMock: flowRepoMock);

        var result = await service.GetPublicBySlugAsync("test-wedding", null);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.RequiresPasscode);
        Assert.Single(result.Value.Website!.Events!);
        Assert.Equal("Reception", result.Value.Website.Events![0].Name);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_DeletesWebsite()
    {
        var weddingId = Guid.NewGuid();
        var website = CreateWebsiteEntity(weddingId);
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync(website);
        repoMock.Setup(r => r.DeleteAsync(website.Id)).Returns(Task.CompletedTask);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.DeleteAsync(weddingId);

        Assert.True(result.IsSuccess);
        repoMock.Verify(r => r.DeleteAsync(website.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFoundWhenMissing()
    {
        var weddingId = Guid.NewGuid();
        var repoMock = new Mock<IWeddingWebsiteRepository>();
        repoMock.Setup(r => r.GetByWeddingIdAsync(weddingId)).ReturnsAsync((WeddingWebsite?)null);
        var service = CreateService(websiteRepoMock: repoMock);

        var result = await service.DeleteAsync(weddingId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }
}
