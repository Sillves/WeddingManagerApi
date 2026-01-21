using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.Weddings;

public class GetWeddings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/weddings", async (IWeddingService weddingService) =>
        {
            var weddings = await weddingService.GetAllAsync();
            return Results.Ok(weddings); // TODO get returns weird data
        })
        .WithTags("Weddings")
        .WithName("GetWeddings")
        .WithOpenApi()
        .RequireAuthorization();
    }
}
