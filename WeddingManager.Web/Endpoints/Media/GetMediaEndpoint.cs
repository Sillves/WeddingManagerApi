using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;

namespace WeddingManager.Web.Endpoints.Media;

public class GetMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/media/{mediaId:guid}",
                async (Guid mediaId, IMediaService mediaService, HttpContext httpContext) =>
                {
                    var result = await mediaService.GetStreamAsync(mediaId);

                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    var (stream, contentType, fileName) = result.Value;

                    // Set cache headers for browser caching (1 week)
                    httpContext.Response.Headers.CacheControl = "public, max-age=604800";

                    return Results.File(stream, contentType, enableRangeProcessing: true);
                })
            .WithTags("Media")
            .WithName("GetMedia")
            .AllowAnonymous() // Allow public access for wedding websites
            .RequireRateLimiting("PublicApi")
            .Produces(200)
            .Produces(404);
    }
}
