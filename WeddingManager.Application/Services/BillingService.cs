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

        var price = await ResolvePriceAsync(tier, interval);
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
            Mode = interval == BillingInterval.Lifetime ? "payment" : "subscription",
            SuccessUrl = _stripeSettings.SuccessUrl,
            CancelUrl = _stripeSettings.CancelUrl,
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
                ["tier"] = tier.ToString(),
                ["interval"] = interval.ToString()
            }
        };

        if (interval != BillingInterval.Lifetime)
        {
            options.SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString(),
                    ["tier"] = tier.ToString(),
                    ["interval"] = interval.ToString()
                }
            };
        }

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

    public async Task<Result> ChangePlanAsync(SubscriptionTier tier, BillingInterval interval)
    {
        if (interval == BillingInterval.Lifetime)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Lifetime plans must be purchased through checkout."));
        }

        if (tier == SubscriptionTier.Free)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Free tier does not require a paid subscription."));
        }

        var price = await ResolvePriceAsync(tier, interval);
        if (price == null)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Stripe price mapping not found."));
        }

        var userId = userContextService.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "User not found."));
        }

        if (string.IsNullOrWhiteSpace(user.StripeSubscriptionId))
        {
            return Result.Fail(new Error(ErrorCodes.Conflict, "No active subscription found to update."));
        }

        var subscriptionService = new SubscriptionService(_stripeClient);
        var subscription = await subscriptionService.GetAsync(user.StripeSubscriptionId);
        var item = subscription.Items.Data.FirstOrDefault();
        if (item == null)
        {
            return Result.Fail(new Error(ErrorCodes.ExternalFailure, "Stripe subscription items not found."));
        }

        var updateOptions = new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = false,
            ProrationBehavior = "create_prorations",
            Metadata = new Dictionary<string, string>
            {
                ["tier"] = tier.ToString(),
                ["interval"] = interval.ToString()
            },
            Items =
            [
                new SubscriptionItemOptions
                {
                    Id = item.Id,
                    Price = price.Id
                }
            ]
        };

        var updated = await subscriptionService.UpdateAsync(subscription.Id, updateOptions);
        return await UpdateUserSubscriptionAsync(user, tier, updated.CustomerId, updated.Id);
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

        return stripeEvent.Type switch
        {
            "checkout.session.completed" => await HandleCheckoutSessionCompletedAsync(stripeEvent),
            "invoice.paid" => await HandleInvoicePaidAsync(stripeEvent),
            "invoice.payment_failed" => await HandleInvoicePaymentFailedAsync(stripeEvent),
            "customer.subscription.updated" => await HandleSubscriptionUpdatedAsync(stripeEvent),
            "customer.subscription.deleted" => await HandleSubscriptionDeletedAsync(stripeEvent),
            _ => Result.Ok()
        };
    }

    public async Task<Result<IReadOnlyList<BillingPlanDto>>> GetPlansAsync()
    {
        var tiers = Enum.GetValues<SubscriptionTier>();
        var plans = new List<BillingPlanDto>();

        foreach (var tier in tiers)
        {
            var limits = _subscriptionPlanOptions.GetLimits(tier);
            var prices = new List<BillingPlanPriceDto>();

            foreach (var interval in new[] { BillingInterval.Monthly, BillingInterval.Annual, BillingInterval.Lifetime })
            {
                var price = await ResolvePriceAsync(tier, interval);
                if (price == null)
                {
                    continue;
                }

                var unitAmount = price.UnitAmount ?? 0;
                var currency = price.Currency ?? "usd";
                var isRecurring = price.Recurring != null;

                prices.Add(new BillingPlanPriceDto(price.Id, interval, unitAmount, currency, isRecurring));
            }

            plans.Add(new BillingPlanDto(
                tier,
                tier.ToString(),
                limits.Features,
                limits.MaxGuests,
                limits.MaxEvents,
                limits.MaxEmailsPerMonth,
                prices));
        }

        return Result<IReadOnlyList<BillingPlanDto>>.Ok(plans);
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
        if (tier == null && !string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            tier = await GetTierFromSubscriptionAsync(session.SubscriptionId);
        }

        if (tier != null)
            return await UpdateUserSubscriptionAsync(user, tier.Value, session.CustomerId, session.SubscriptionId);
        
        logger.LogWarning("Stripe checkout session completed but tier could not be resolved. SessionId: {SessionId}", session.Id);
        return Result.Ok();

    }

    private async Task<Result> HandleInvoicePaidAsync(StripeEvent stripeEvent)
    {
        if (stripeEvent.Data.Object is not Invoice invoice)
        {
            return Result.Ok();
        }

        var linePrice = invoice.Lines?.Data.FirstOrDefault()?.Price;
        var tier = _stripeSettings.GetTierForPriceId(linePrice?.Id, linePrice?.ProductId);
        if (tier == null && !string.IsNullOrWhiteSpace(invoice.SubscriptionId))
        {
            tier = await GetTierFromSubscriptionAsync(invoice.SubscriptionId);
        }

        if (tier == null)
        {
            logger.LogWarning("Stripe invoice paid but tier could not be resolved. InvoiceId: {InvoiceId}", invoice.Id);
            return Result.Ok();
        }

        var user = await FindUserAsync(GetMetadataValue(invoice.Metadata, "userId"), invoice.CustomerId);
        if (user != null)
            return await UpdateUserSubscriptionAsync(user, tier.Value, invoice.CustomerId, invoice.SubscriptionId);
        
        logger.LogWarning("Stripe invoice paid but no user found. CustomerId: {CustomerId}", invoice.CustomerId);
        return Result.Ok();

    }

    private async Task<Result> HandleInvoicePaymentFailedAsync(StripeEvent stripeEvent)
    {
        if (stripeEvent.Data.Object is not Invoice invoice)
        {
            return Result.Ok();
        }

        var user = await FindUserAsync(GetMetadataValue(invoice.Metadata, "userId"), invoice.CustomerId);
            
        if (user != null) return await DowngradeToFreeAsync(user, invoice.CustomerId);
        logger.LogWarning("Stripe invoice payment failed but no user found. CustomerId: {CustomerId}", invoice.CustomerId);
        
        return Result.Ok();

    }

    private async Task<Result> HandleSubscriptionUpdatedAsync(StripeEvent stripeEvent)
    {
        if (stripeEvent.Data.Object is not Subscription subscription)
        {
            return Result.Ok();
        }

        var user = await FindUserAsync(GetMetadataValue(subscription.Metadata, "userId"), subscription.CustomerId);
        if (user == null)
        {
            logger.LogWarning("Stripe subscription updated but no user found. CustomerId: {CustomerId}", subscription.CustomerId);
            return Result.Ok();
        }

        if (subscription.Status is not "active" and not "trialing")
        {
            return await DowngradeToFreeAsync(user, subscription.CustomerId);
        }

        var itemPrice = subscription.Items.Data.FirstOrDefault()?.Price;
        var tier = GetTierFromMetadata(subscription.Metadata)
                   ?? _stripeSettings.GetTierForPriceId(itemPrice?.Id, itemPrice?.ProductId);

        if (tier != null)
            return await UpdateUserSubscriptionAsync(user, tier.Value, subscription.CustomerId, subscription.Id);
        
        logger.LogWarning("Stripe subscription updated but tier could not be resolved. SubscriptionId: {SubscriptionId}", subscription.Id);
        return Result.Ok();

    }

    private async Task<Result> HandleSubscriptionDeletedAsync(StripeEvent stripeEvent)
    {
        if (stripeEvent.Data.Object is not Subscription subscription)
        {
            return Result.Ok();
        }

        var user = await FindUserAsync(GetMetadataValue(subscription.Metadata, "userId"), subscription.CustomerId);
        if (user != null) return await DowngradeToFreeAsync(user, subscription.CustomerId);
            
        logger.LogWarning("Stripe subscription deleted but no user found. CustomerId: {CustomerId}", subscription.CustomerId);
        
        return Result.Ok();

    }

    private async Task<SubscriptionTier?> GetTierFromSubscriptionAsync(string subscriptionId)
    {
        var subscriptionService = new SubscriptionService(_stripeClient);
        var subscription = await subscriptionService.GetAsync(subscriptionId);
        var price = subscription.Items.Data.FirstOrDefault()?.Price;
        return _stripeSettings.GetTierForPriceId(price?.Id, price?.ProductId);
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

    private async Task<Result> UpdateUserSubscriptionAsync(User user, SubscriptionTier tier, string? customerId, string? subscriptionId)
    {
        user.SubscriptionTier = tier;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            user.StripeCustomerId = customerId;
        }

        if (!string.IsNullOrWhiteSpace(subscriptionId))
        {
            user.StripeSubscriptionId = subscriptionId;
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail(new Error(ErrorCodes.ExternalFailure, "Failed to update subscription details."));
        }

        return Result.Ok();
    }

    private async Task<Result> DowngradeToFreeAsync(User user, string? customerId)
    {
        user.SubscriptionTier = SubscriptionTier.Free;
        user.StripeSubscriptionId = null;
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

    private async Task<Price?> ResolvePriceAsync(SubscriptionTier tier, BillingInterval interval)
    {
        var configuredId = _stripeSettings.GetConfiguredId(tier, interval);
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

        var listOptions = new PriceListOptions
        {
            Product = configuredId,
            Active = true,
            Limit = 100
        };

        var prices = await priceService.ListAsync(listOptions);
        return interval switch
        {
            BillingInterval.Monthly => prices.Data.FirstOrDefault(p => p.Recurring?.Interval == "month"),
            BillingInterval.Annual => prices.Data.FirstOrDefault(p => p.Recurring?.Interval == "year"),
            BillingInterval.Lifetime => prices.Data.FirstOrDefault(p => p.Recurring == null),
            _ => null
        };
    }
}
