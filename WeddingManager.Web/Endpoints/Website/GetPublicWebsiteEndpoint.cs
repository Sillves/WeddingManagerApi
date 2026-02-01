using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Website;

public class GetPublicWebsiteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/w/{slug}",
                async (string slug, IWeddingWebsiteService websiteService) =>
                {
                    var result = await websiteService.GetPublicBySlugAsync(slug);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Public Website")
            .WithName("GetPublicWebsite")
            .AllowAnonymous()
            .RequireRateLimiting("PublicApi")
            .Produces<PublicWeddingWebsiteDto>(200)
            .Produces<ErrorResponse>(404);
    }
}
