using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Utils;

public class SubscriptionPlanOptions
{
    public Dictionary<string, SubscriptionPlanLimits> Plans { get; set; } = new();

    public SubscriptionPlanLimits GetLimits(SubscriptionTier tier)
    {
        var key = tier.ToString();
        if (Plans.TryGetValue(key, out var limits))
        {
            return limits;
        }

        throw new InvalidOperationException($"No subscription plan limits configured for tier '{key}'.");
    }
}

public class SubscriptionPlanLimits
{
    public int MaxGuests { get; set; }
    public int MaxEvents { get; set; }
    public int MaxEmailsPerMonth { get; set; }
    public List<string> Features { get; set; } = new();
}
