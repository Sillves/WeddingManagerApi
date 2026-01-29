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

        return interval switch
        {
            BillingInterval.Monthly => options.Monthly,
            BillingInterval.Annual => options.Annual,
            BillingInterval.Lifetime => options.Lifetime,
            _ => null
        };
    }

    public SubscriptionTier? GetTierForPriceId(string? priceId, string? productId = null)
    {
        if (string.IsNullOrWhiteSpace(priceId) && string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        foreach (var (tierKey, options) in Prices)
        {
            if (string.Equals(options.Monthly, priceId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options.Annual, priceId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options.Lifetime, priceId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options.Monthly, productId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options.Annual, productId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options.Lifetime, productId, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.TryParse<SubscriptionTier>(tierKey, true, out var tier) ? tier : null;
            }
        }

        return null;
    }

    public static bool IsProductId(string? id) => !string.IsNullOrWhiteSpace(id) && id.StartsWith("prod_", StringComparison.OrdinalIgnoreCase);
}

public class StripePriceOptions
{
    public string Monthly { get; set; } = string.Empty;
    public string Annual { get; set; } = string.Empty;
    public string Lifetime { get; set; } = string.Empty;
}
