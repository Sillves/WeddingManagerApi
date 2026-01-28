using WeddingManager.Domain.Exceptions;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Guests;

public class SendGuestInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests/{guestId}/send-invitation",
                async (Guid weddingId, Guid guestId, IGuestService guestService) =>
                {
                    try
                    {
                        await guestService.SendInvitationAsync(weddingId, guestId);
                        return Results.Ok();
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return Results.NotFound(new { error = ex.Message });
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }
                    catch (SubscriptionLimitExceededException ex)
                    {
                        return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
            .WithTags("Guests")
            .WithName("SendGuestInvitation")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireRateLimiting("InvitationSend")
            .Produces(200)
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .Produces(429)
            .Produces(500);
    }
}
