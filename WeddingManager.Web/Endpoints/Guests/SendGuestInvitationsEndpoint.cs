using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Guests;

public class SendGuestInvitationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests/send-invitations",
                async (Guid weddingId, SendInvitationsRequestDto? requestDto, IGuestService guestService) =>
                {
                    try
                    {
                        var guestIds = requestDto?.GuestIds;
                        var result = await guestService.SendInvitationsAsync(weddingId, guestIds);
                        return Results.Ok(result);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return Results.NotFound(new { error = ex.Message });
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
            .WithTags("Guests")
            .WithName("SendGuestInvitations")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireRateLimiting("InvitationSend")
            .Produces<InvitationSendResultDto>(200)
            .Produces(400)
            .Produces(404)
            .Produces(429)
            .Produces(500);
    }
}
