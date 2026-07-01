using Microsoft.AspNetCore.DataProtection;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Rsvp;

public class GetRsvpFlowStateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/weddings/{slug}/rsvp",
                async (string slug, HttpContext http, IRsvpService rsvpService, IDataProtectionProvider dp) =>
                {
                    var unlockedFlowId = RsvpFlowCookie.ResolveFlowId(http, dp);
                    var result = await rsvpService.GetFlowStateAsync(slug, unlockedFlowId);
                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    // Open (no-passcode) flow: issue the ticket immediately so submit is uniform.
                    var state = result.Value!;
                    if (state is { RequiresPasscode: false, Flow: not null })
                    {
                        RsvpFlowCookie.Issue(http, dp, state.Flow.WeddingId, state.Flow.FlowId);
                    }

                    return Results.Ok(state);
                })
            .WithTags("Rsvp")
            .WithName("GetRsvpFlowState")
            .AllowAnonymous()
            .RequireRateLimiting("PublicApi")
            .Produces<RsvpFlowStateDto>(200)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(429);
    }
}
