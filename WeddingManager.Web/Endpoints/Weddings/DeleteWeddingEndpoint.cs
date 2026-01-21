using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.Weddings;

public class DeleteWeddingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/weddings/{id}", async (Guid id, IWeddingService weddingService) =>
        {
            await weddingService.DeleteAsync(id);
            return Results.NoContent();
        })
        .WithTags("Weddings")
        .WithName("DeleteWedding")
        .WithOpenApi()
        .RequireAuthorization();
    }
}