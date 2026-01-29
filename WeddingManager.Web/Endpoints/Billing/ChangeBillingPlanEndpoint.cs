using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Billing;

public class ChangeBillingPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/billing/change-plan", async (BillingPlanRequest request, IBillingService billingService) =>
            {
                var result = await billingService.ChangePlanAsync(request.Tier, request.Interval);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                return Results.Ok();
            })
            .WithTags("Billing")
            .WithName("ChangeBillingPlan")
            .RequireAuthorization()
            .Produces(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(409);
    }
}
