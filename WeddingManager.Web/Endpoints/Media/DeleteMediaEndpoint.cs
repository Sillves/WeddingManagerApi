using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Media;

public class DeleteMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/weddings/{weddingId}/media/{mediaId}",
                async (Guid weddingId, Guid mediaId, IMediaService mediaService) =>
                {
                    var result = await mediaService.DeleteAsync(mediaId, weddingId);
                    return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
                })
            .WithTags("Media")
            .WithName("DeleteMedia")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
