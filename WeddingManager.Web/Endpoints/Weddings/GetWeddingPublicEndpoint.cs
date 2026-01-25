using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.Weddings;

public class GetWeddingPublicEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{idOrSlug}/public",
            async (string idOrSlug, IWeddingService weddingService, IMapper mapper) =>
            {
                var wedding = await weddingService.GetByIdOrSlugAsync(idOrSlug);
                if (wedding == null)
                {
                    return Results.NotFound();
                }

                var dto = mapper.Map<WeddingPublicDto>(wedding);
                return Results.Ok(dto);
            })
            .WithTags("Weddings")
            .WithName("GetWeddingPublicInfo")
            .WithOpenApi()
            .AllowAnonymous()
            .RequireRateLimiting("PublicWedding")
            .Produces<WeddingPublicDto>(200)
            .Produces(404)
            .Produces(429);
    }
}
