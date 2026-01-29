using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Billing;

public class StripeWebhookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/billing/webhook", async (HttpRequest request, IBillingService billingService) =>
            {
                var signature = request.Headers["Stripe-Signature"].ToString();
                if (string.IsNullOrWhiteSpace(signature))
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Errors = [new Error(ErrorCodes.Validation, "Stripe signature header missing.")]
                    });
                }

                using var reader = new StreamReader(request.Body);
                var payload = await reader.ReadToEndAsync();

                var result = await billingService.HandleWebhookAsync(payload, signature);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                return Results.Ok();
            })
            .WithTags("Billing")
            .WithName("StripeWebhook")
            .AllowAnonymous()
            .Produces(200)
            .Produces<ErrorResponse>(400);
    }
}
