using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Guests;

public class CheckEmailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests/check-emails",
            async (Guid weddingId, CheckEmailsRequest request, IGuestService guestService) =>
            {
                var result = await guestService.CheckExistingEmailsAsync(weddingId, request.Emails);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                return Results.Ok(new CheckEmailsResponse { ExistingEmails = result.Value!.ToList() });
            })
            .WithTags("Guests")
            .WithName("CheckEmails")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<CheckEmailsResponse>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403);
    }
}

public record CheckEmailsRequest
{
    public List<string> Emails { get; init; } = [];
}

public record CheckEmailsResponse
{
    public List<string> ExistingEmails { get; init; } = [];
}
