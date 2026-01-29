using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Billing;

public class GetBillingPlansEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/billing/plans", async (IBillingService billingService) =>
            {
                var result = await billingService.GetPlansAsync();
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                return Results.Ok(result.Value);
            })
            .WithTags("Billing")
            .WithName("GetBillingPlans")
            .AllowAnonymous()
            .Produces<IReadOnlyList<BillingPlanDto>>(200)
            .Produces<ErrorResponse>(400);
    }
}
