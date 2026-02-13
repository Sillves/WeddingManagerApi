using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Guests;

public class SendGuestInvitationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests/send-invitations",
                async (Guid weddingId, SendInvitationsRequestDto? requestDto, IGuestService guestService) =>
                {
                    var guestIds = requestDto?.GuestIds;
                    var result = await guestService.SendInvitationsAsync(weddingId, guestIds);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Guests")
            .WithName("SendGuestInvitations")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .RequireRateLimiting("InvitationSend")
            .Produces<InvitationSendResultDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(429)
            .Produces<ErrorResponse>(500)
            .Produces<ErrorResponse>(502);
    }
}
