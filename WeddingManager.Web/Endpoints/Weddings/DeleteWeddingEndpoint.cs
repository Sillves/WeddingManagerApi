using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class DeleteWeddingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/weddings/{id}", async (Guid id, IWeddingService weddingService) =>
        {
            var result = await weddingService.DeleteAsync(id);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .WithTags("Weddings")
        .WithName("DeleteWedding")
        .RequireAuthorization()
        .Produces(204)
        .Produces<ErrorResponse>(401)
        .Produces<ErrorResponse>(404);
    }
}
