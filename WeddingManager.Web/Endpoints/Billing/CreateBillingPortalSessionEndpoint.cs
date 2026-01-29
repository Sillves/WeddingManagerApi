using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Billing;

public class CreateBillingPortalSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/billing/portal-session", async (IBillingService billingService) =>
            {
                var result = await billingService.CreatePortalSessionAsync();
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                return Results.Ok(result.Value);
            })
            .WithTags("Billing")
            .WithName("CreateBillingPortalSession")
            .RequireAuthorization()
            .Produces<BillingPortalSession>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(409);
    }
}
