using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Media;

public class GetWeddingMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/media",
                async (Guid weddingId, IMediaService mediaService) =>
                {
                    var result = await mediaService.GetByWeddingIdAsync(weddingId);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Media")
            .WithName("GetWeddingMedia")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<IEnumerable<MediaUploadResponseDto>>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403);
    }
}
