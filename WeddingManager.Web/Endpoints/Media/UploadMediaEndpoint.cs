using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Media;

public class UploadMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/media",
                async (Guid weddingId, IFormFile file, IMediaService mediaService) =>
                {
                    await using var stream = file.OpenReadStream();
                    var result = await mediaService.UploadAsync(
                        weddingId,
                        stream,
                        file.FileName,
                        file.ContentType,
                        file.Length);

                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    return Results.Created($"/api/media/{result.Value!.Id}", result.Value);
                })
            .WithTags("Media")
            .WithName("UploadMedia")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .DisableAntiforgery()
            .Produces<MediaUploadResponseDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(413);
    }
}
