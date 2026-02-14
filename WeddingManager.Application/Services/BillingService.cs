using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Domain.Utils;
using BillingPortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using BillingPortalSessionService = Stripe.BillingPortal.SessionService;
using CheckoutSession = Stripe.Checkout.Session;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using StripeEvent = Stripe.Event;

namespace WeddingManager.Application.Services;

public class BillingService(
    UserManager<User> userManager,
    IUserContextService userContextService,
    IOptions<StripeSettings> stripeOptions,
    IOptions<SubscriptionPlanOptions> subscriptionPlanOptions,
    IReferralService referralService,
    ILogger<BillingService> logger) : IBillingService
{
    private readonly StripeSettings _stripeSettings = stripeOptions.Value;
    private readonly SubscriptionPlanOptions _subscriptionPlanOptions = subscriptionPlanOptions.Value;
    private readonly StripeClient _stripeClient = new(stripeOptions.Value.SecretKey);

    public async Task<Result<BillingCheckoutSession>> CreateCheckoutSessionAsync(SubscriptionTier tier, BillingInterval interval)
    {
        if (tier == SubscriptionTier.Free)
        {
            return Result<BillingCheckoutSession>.Fail(new Error(ErrorCodes.Validation, "Free tier does not require checkout."));
        }

        var price = await ResolvePriceAsync(tier);
        if (price == null)
        {
            return Result<BillingCheckoutSession>.Fail(new Error(ErrorCodes.Validation, "Stripe price mapping not found."));
        }

        var userId = userContextService.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<BillingCheckoutSession>.Fail(new Error(ErrorCodes.NotFound, "User not found."));
        }

        var options = new CheckoutSessionCreateOptions
        {
            Mode = "payment", // One-time payment only
            SuccessUrl = _stripeSettings.SuccessUrl,
            CancelUrl = _stripeSettings.CancelUrl,
            AllowPromotionCodes = true,
            ClientReferenceId = userId.ToString(),
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = price.Id,
                    Quantity = 1
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["tier"] = tier.ToString()
            }
        };

        if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            options.Customer = user.StripeCustomerId;
        }
        else if (!string.IsNullOrWhiteSpace(user.Email))
        {
            options.CustomerEmail = user.Email;
        }

        var sessionService = new CheckoutSessionService(_stripeClient);
        var session = await sessionService.CreateAsync(options);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            return Result<BillingCheckoutSession>.Fail(new Error(ErrorCodes.ExternalFailure, "Stripe did not return a checkout URL."));
        }

        return Result<BillingCheckoutSession>.Ok(new BillingCheckoutSession(session.Id, session.Url));
    }

    public async Task<Result<BillingPortalSession>> CreatePortalSessionAsync()
    {
        var userId = userContextService.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<BillingPortalSession>.Fail(new Error(ErrorCodes.NotFound, "User not found."));
        }

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            return Result<BillingPortalSession>.Fail(new Error(ErrorCodes.Conflict, "No Stripe customer found for this user."));
        }

        var portalService = new BillingPortalSessionService(_stripeClient);
        var options = new BillingPortalSessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            ReturnUrl = _stripeSettings.PortalReturnUrl
        };

        var session = await portalService.CreateAsync(options);
        if (string.IsNullOrWhiteSpace(session.Url))
        {
            return Result<BillingPortalSession>.Fail(new Error(ErrorCodes.ExternalFailure, "Stripe did not return a portal URL."));
        }

        return Result<BillingPortalSession>.Ok(new BillingPortalSession(session.Id, session.Url));
    }

    public async Task<Result> HandleWebhookAsync(string payload, string signature)
    {
        StripeEvent stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _stripeSettings.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return Result.Fail(new Error(ErrorCodes.Validation, "Invalid Stripe signature."));
        }

        // Only handle checkout.session.completed for one-time payments
        return stripeEvent.Type switch
        {
            "checkout.session.completed" => await HandleCheckoutSessionCompletedAsync(stripeEvent),
            _ => Result.Ok()
        };
    }

    public async Task<Result<IReadOnlyList<BillingPlanDto>>> GetPlansAsync()
    {
        var tiers = Enum.GetValues<SubscriptionTier>();
        var plans = new List<BillingPlanDto>();
        var featureUniverse = BuildFeatureUniverse(tiers);

        foreach (var tier in tiers)
        {
            var limits = _subscriptionPlanOptions.GetLimits(tier);
            var prices = new List<BillingPlanPriceDto>();
            var notIncluded = featureUniverse
                .Where(feature => !limits.Features.Contains(feature, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Only fetch one-time (Lifetime) price
            var price = await ResolvePriceAsync(tier);
            if (price != null)
            {
                var unitAmount = price.UnitAmount ?? 0;
                var currency = price.Currency ?? "eur";
                prices.Add(new BillingPlanPriceDto(price.Id, BillingInterval.Lifetime, unitAmount, currency, false));
            }

            plans.Add(new BillingPlanDto(
                tier,
                tier.ToString(),
                limits.Features,
                notIncluded,
                limits.MaxGuests,
                limits.MaxEvents,
                limits.MaxEmailsPerMonth,
                prices));
        }

        return Result<IReadOnlyList<BillingPlanDto>>.Ok(plans);
    }

    private List<string> BuildFeatureUniverse(IEnumerable<SubscriptionTier> tiers)
    {
        var features = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tier in tiers)
        {
            var limits = _subscriptionPlanOptions.GetLimits(tier);
            foreach (var feature in limits.Features)
            {
                if (seen.Add(feature))
                {
                    features.Add(feature);
                }
            }
        }

        return features;
    }

    private async Task<Result> HandleCheckoutSessionCompletedAsync(StripeEvent stripeEvent)
    {
        if (stripeEvent.Data.Object is not CheckoutSession session)
        {
            return Result.Ok();
        }

        var userId = session.ClientReferenceId ?? GetMetadataValue(session.Metadata, "userId");
        var user = await FindUserAsync(userId, session.CustomerId);
        if (user == null)
        {
            logger.LogWarning("Stripe checkout session completed but no user found. CustomerId: {CustomerId}", session.CustomerId);
            return Result.Ok();
        }

        var tier = GetTierFromMetadata(session.Metadata);
        if (tier != null)
        {
            var updateResult = await UpdateUserTierAsync(user, tier.Value, session.CustomerId);
            // Track referral conversion on successful payment
            if (updateResult.IsSuccess)
                await referralService.TrackReferralConversionAsync(user.Id);
            return updateResult;
        }

        logger.LogWarning("Stripe checkout session completed but tier could not be resolved. SessionId: {SessionId}", session.Id);
        return Result.Ok();
    }

    private async Task<User?> FindUserAsync(string? userId, string? customerId)
    {
        if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var id))
        {
            var byId = await userManager.FindByIdAsync(id.ToString());
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            return await userManager.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        }

        return null;
    }

    private async Task<Result> UpdateUserTierAsync(User user, SubscriptionTier tier, string? customerId)
    {
        user.SubscriptionTier = tier;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            user.StripeCustomerId = customerId;
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail(new Error(ErrorCodes.ExternalFailure, "Failed to update subscription details."));
        }

        return Result.Ok();
    }

    private static SubscriptionTier? GetTierFromMetadata(IDictionary<string, string>? metadata)
    {
        var value = GetMetadataValue(metadata, "tier");
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<SubscriptionTier>(value, true, out var tier) ? tier : null;
    }

    private static string? GetMetadataValue(IDictionary<string, string>? metadata, string key)
    {
        if (metadata == null)
        {
            return null;
        }

        return metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Resolves the one-time (lifetime) price for a subscription tier.
    /// </summary>
    private async Task<Price?> ResolvePriceAsync(SubscriptionTier tier)
    {
        var configuredId = _stripeSettings.GetConfiguredId(tier, BillingInterval.Lifetime);
        if (string.IsNullOrWhiteSpace(configuredId))
        {
            return null;
        }

        var priceService = new PriceService(_stripeClient);

        if (!StripeSettings.IsProductId(configuredId))
        {
            try
            {
                return await priceService.GetAsync(configuredId);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Failed to load Stripe price {PriceId}", configuredId);
                return null;
            }
        }

        // For product IDs, find the one-time (non-recurring) price
        var listOptions = new PriceListOptions
        {
            Product = configuredId,
            Active = true,
            Limit = 100
        };

        var prices = await priceService.ListAsync(listOptions);
        return prices.Data.FirstOrDefault(p => p.Recurring == null);
    }
}
