using Microsoft.AspNetCore.DataProtection;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Endpoints.Rsvp;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Website;

public class GetPublicWebsiteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/w/{slug}",
                async (string slug, HttpContext http, IWeddingWebsiteService websiteService,
                    IDataProtectionProvider dp) =>
                {
                    // Reuse the shared flow-session cookie: unlocking the RSVP flow (or the website
                    // gate) with a passcode issues it, and it governs website access + event filtering.
                    var flowId = RsvpFlowCookie.ResolveFlowId(http, dp);
                    var result = await websiteService.GetPublicBySlugAsync(slug, flowId);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Public Website")
            .WithName("GetPublicWebsite")
            .AllowAnonymous()
            .RequireRateLimiting("PublicApi")
            .Produces<PublicWebsiteResponseDto>(200)
            .Produces<ErrorResponse>(404);
    }
}
