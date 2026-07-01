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
                async (string slug, HttpContext http, IWeddingWebsiteService websiteService, IDataProtectionProvider dp) =>
                {
                    var unlockedFlowId = RsvpFlowCookie.ResolveFlowId(http, dp);
                    var result = await websiteService.GetPublicBySlugAsync(slug, unlockedFlowId);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Public Website")
            .WithName("GetPublicWebsite")
            .AllowAnonymous()
            .RequireRateLimiting("PublicApi")
            .Produces<PublicWebsiteStateDto>(200)
            .Produces<ErrorResponse>(404);
    }
}
