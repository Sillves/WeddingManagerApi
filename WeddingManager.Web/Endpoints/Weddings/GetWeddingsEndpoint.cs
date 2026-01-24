using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.Weddings;

public class GetWeddings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings", async (IWeddingService weddingService, IMapper mapper) =>
        {
            var weddings = await weddingService.GetAllAsync();
            var dtos = mapper.Map<IEnumerable<WeddingDto>>(weddings);
            return Results.Ok(dtos);
        })
        .WithTags("Weddings")
        .WithName("GetWeddings")
        .WithOpenApi()
        .RequireAuthorization();
    }
}
