using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Guests;

public class SendGuestInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests/{guestId}/send-invitation",
                async (Guid weddingId, Guid guestId, IGuestService guestService) =>
                {
                    var result = await guestService.SendInvitationAsync(weddingId, guestId);
                    return result.IsSuccess ? Results.Ok() : result.ToErrorResult();
                })
            .WithTags("Guests")
            .WithName("SendGuestInvitation")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .RequireRateLimiting("InvitationSend")
            .Produces(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(429)
            .Produces<ErrorResponse>(500)
            .Produces<ErrorResponse>(502);
    }
}
