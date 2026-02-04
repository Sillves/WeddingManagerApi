using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Utils;

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string PortalReturnUrl { get; set; } = string.Empty;
    public Dictionary<string, StripePriceOptions> Prices { get; set; } = new();

    /// <summary>
    /// Gets the configured Stripe price ID for a subscription tier.
    /// Only lifetime (one-time) payments are supported.
    /// </summary>
    public string? GetConfiguredId(SubscriptionTier tier, BillingInterval interval)
    {
        if (tier == SubscriptionTier.Free)
        {
            return null;
        }

        var key = tier.ToString();
        if (!Prices.TryGetValue(key, out var options))
        {
            return null;
        }

        // Only lifetime (one-time) payments are supported
        return options.Lifetime;
    }

    /// <summary>
    /// Gets the subscription tier for a Stripe price or product ID.
    /// </summary>
    public SubscriptionTier? GetTierForPriceId(string? priceId, string? productId = null)
    {
        if (string.IsNullOrWhiteSpace(priceId) && string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        foreach (var (tierKey, options) in Prices)
        {
            if (string.Equals(options.Lifetime, priceId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options.Lifetime, productId, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.TryParse<SubscriptionTier>(tierKey, true, out var tier) ? tier : null;
            }
        }

        return null;
    }

    public static bool IsProductId(string? id) => !string.IsNullOrWhiteSpace(id) && id.StartsWith("prod_", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Stripe price configuration for a subscription tier.
/// Only lifetime (one-time) payments are supported.
/// </summary>
public class StripePriceOptions
{
    /// <summary>
    /// Stripe price ID for one-time payment.
    /// Can be a price ID (price_xxx) or product ID (prod_xxx).
    /// </summary>
    public string Lifetime { get; set; } = string.Empty;
}
