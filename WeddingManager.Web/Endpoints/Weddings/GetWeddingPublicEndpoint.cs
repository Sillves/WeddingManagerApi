using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class GetWeddingPublicEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{idOrSlug}/public",
            async (string idOrSlug, IWeddingService weddingService, IMapper mapper) =>
            {
                var result = await weddingService.GetByIdOrSlugAsync(idOrSlug);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                var dto = mapper.Map<WeddingPublicDto>(result.Value);
                return Results.Ok(dto);
            })
            .WithTags("Weddings")
            .WithName("GetWeddingPublicInfo")
            .AllowAnonymous()
            .RequireRateLimiting("PublicWedding")
            .Produces<WeddingPublicDto>(200)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(429);
    }
}
