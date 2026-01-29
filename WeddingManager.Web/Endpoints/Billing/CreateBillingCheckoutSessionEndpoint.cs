using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Billing;

public class CreateBillingCheckoutSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/billing/checkout-session", async (BillingPlanRequest request, IBillingService billingService) =>
            {
                var result = await billingService.CreateCheckoutSessionAsync(request.Tier, request.Interval);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                return Results.Ok(result.Value);
            })
            .WithTags("Billing")
            .WithName("CreateBillingCheckoutSession")
            .RequireAuthorization()
            .Produces<BillingCheckoutSession>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401);
    }
}
