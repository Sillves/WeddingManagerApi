using Microsoft.AspNetCore.DataProtection;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Rsvp;

public class UnlockRsvpFlowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/weddings/{slug}/rsvp/unlock",
                async (string slug, RsvpUnlockRequestDto requestDto, HttpContext http,
                    IRsvpService rsvpService, IDataProtectionProvider dp) =>
                {
                    var result = await rsvpService.UnlockAsync(slug, requestDto.Passcode);
                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    var flow = result.Value!;
                    RsvpFlowCookie.Issue(http, dp, flow.WeddingId, flow.FlowId);
                    return Results.Ok(flow);
                })
            .WithTags("Rsvp")
            .WithName("UnlockRsvpFlow")
            .AllowAnonymous()
            .RequireRateLimiting("RsvpUnlock")
            .Produces<RsvpFlowPublicDto>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(429);
    }
}
