using WeddingManager.Domain.Enums;

namespace WeddingManager.Web.Models;

public record BillingPlanRequest(SubscriptionTier Tier, BillingInterval Interval);
