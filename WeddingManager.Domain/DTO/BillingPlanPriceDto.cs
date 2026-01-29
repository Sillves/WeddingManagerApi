using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public record BillingPlanPriceDto(
    string PriceId,
    BillingInterval Interval,
    long UnitAmount,
    string Currency,
    bool IsRecurring);
