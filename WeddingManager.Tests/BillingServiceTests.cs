using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Tests;

public class BillingServiceTests
{
    [Fact]
    public async Task CreateCheckoutSessionAsync_ReturnsValidationForFreeTier()
    {
        var service = CreateService();

        var result = await service.CreateCheckoutSessionAsync(SubscriptionTier.Free, BillingInterval.Monthly);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ReturnsValidationWhenPriceMissing()
    {
        var service = CreateService(stripeSettings: CreateStripeSettings());

        var result = await service.CreateCheckoutSessionAsync(SubscriptionTier.Starter, BillingInterval.Monthly);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreatePortalSessionAsync_ReturnsNotFoundWhenUserMissing()
    {
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        var service = CreateService(userManagerMock: userManagerMock);

        var result = await service.CreatePortalSessionAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task CreatePortalSessionAsync_ReturnsConflictWhenNoCustomer()
    {
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "user@example.com" });
        var service = CreateService(userManagerMock: userManagerMock);

        var result = await service.CreatePortalSessionAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Conflict, result.Errors[0].Code);
    }

    [Fact]
    public async Task ChangePlanAsync_ReturnsValidationForLifetime()
    {
        var service = CreateService();

        var result = await service.ChangePlanAsync(SubscriptionTier.Starter, BillingInterval.Lifetime);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task ChangePlanAsync_ReturnsValidationForFreeTier()
    {
        var service = CreateService();

        var result = await service.ChangePlanAsync(SubscriptionTier.Free, BillingInterval.Monthly);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task ChangePlanAsync_ReturnsValidationWhenPriceMissing()
    {
        var service = CreateService(stripeSettings: CreateStripeSettings());

        var result = await service.ChangePlanAsync(SubscriptionTier.Starter, BillingInterval.Monthly);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task HandleWebhookAsync_ReturnsValidationWhenSignatureInvalid()
    {
        var service = CreateService(stripeSettings: CreateStripeSettings(webhookSecret: "whsec_test"));

        var result = await service.HandleWebhookAsync("{}", "invalid");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task GetPlansAsync_ReturnsPlanPerTier()
    {
        var planOptions = CreatePlanOptions();
        var service = CreateService(subscriptionPlanOptions: planOptions, stripeSettings: CreateStripeSettings());

        var result = await service.GetPlansAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(Enum.GetValues<SubscriptionTier>().Length, result.Value!.Count);
        var allFeatures = planOptions.Value.Plans
            .OrderBy(kvp => Enum.Parse<SubscriptionTier>(kvp.Key, true))
            .SelectMany(kvp => kvp.Value.Features)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Assert.All(result.Value!, plan =>
        {
            var limits = planOptions.Value.GetLimits(plan.Tier);
            Assert.Equal(limits.MaxGuests, plan.MaxGuests);
            Assert.Equal(limits.MaxEvents, plan.MaxEvents);
            Assert.Equal(limits.MaxEmailsPerMonth, plan.MaxEmailsPerMonth);
            var expectedNotIncluded = allFeatures
                .Where(feature => !limits.Features.Contains(feature, StringComparer.OrdinalIgnoreCase))
                .ToList();
            Assert.Equal(expectedNotIncluded, plan.NotIncludedFeatures);
        });
    }

    private static BillingService CreateService(
        Mock<UserManager<User>>? userManagerMock = null,
        Mock<IUserContextService>? userContextMock = null,
        IOptions<StripeSettings>? stripeSettings = null,
        IOptions<SubscriptionPlanOptions>? subscriptionPlanOptions = null)
    {
        userManagerMock ??= CreateUserManager();
        userContextMock ??= new Mock<IUserContextService>();
        userContextMock.Setup(c => c.GetUserId()).Returns(Guid.NewGuid());
        stripeSettings ??= CreateStripeSettings();
        subscriptionPlanOptions ??= CreatePlanOptions();
        var logger = new Mock<ILogger<BillingService>>();
        return new BillingService(
            userManagerMock.Object,
            userContextMock.Object,
            stripeSettings,
            subscriptionPlanOptions,
            logger.Object);
    }

    private static Mock<UserManager<User>> CreateUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static IOptions<StripeSettings> CreateStripeSettings(string? webhookSecret = null)
    {
        return Options.Create(new StripeSettings
        {
            SecretKey = "sk_test_123",
            WebhookSecret = webhookSecret ?? "whsec_default",
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel",
            PortalReturnUrl = "https://example.com/portal",
            Prices = new Dictionary<string, StripePriceOptions>()
        });
    }

    private static IOptions<SubscriptionPlanOptions> CreatePlanOptions()
    {
        var plans = new Dictionary<string, SubscriptionPlanLimits>
        {
            [SubscriptionTier.Free.ToString()] = new SubscriptionPlanLimits
            {
                MaxGuests = 5,
                MaxEvents = 3,
                MaxEmailsPerMonth = 10,
                Features = ["Basic"]
            },
            [SubscriptionTier.Starter.ToString()] = new SubscriptionPlanLimits
            {
                MaxGuests = 50,
                MaxEvents = 10,
                MaxEmailsPerMonth = 100,
                Features = ["Starter"]
            },
            [SubscriptionTier.Pro.ToString()] = new SubscriptionPlanLimits
            {
                MaxGuests = -1,
                MaxEvents = -1,
                MaxEmailsPerMonth = -1,
                Features = ["Pro"]
            }
        };

        return Options.Create(new SubscriptionPlanOptions { Plans = plans });
    }
}
