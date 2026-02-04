using WeddingManager.Domain.Enums;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IBillingService
{
    Task<Result<BillingCheckoutSession>> CreateCheckoutSessionAsync(SubscriptionTier tier, BillingInterval interval);
    Task<Result<BillingPortalSession>> CreatePortalSessionAsync();
    Task<Result<IReadOnlyList<BillingPlanDto>>> GetPlansAsync();
    Task<Result> HandleWebhookAsync(string payload, string signature);
}
