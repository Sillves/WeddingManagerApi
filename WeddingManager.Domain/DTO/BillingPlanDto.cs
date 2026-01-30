using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public record BillingPlanDto(
    SubscriptionTier Tier,
    string Name,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> NotIncludedFeatures,
    int MaxGuests,
    int MaxEvents,
    int MaxEmailsPerMonth,
    IReadOnlyList<BillingPlanPriceDto> Prices);
