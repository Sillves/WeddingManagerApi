using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class GetWeddings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings", async (IWeddingService weddingService) =>
        {
            var result = await weddingService.GetAllWithRoleAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .WithTags("Weddings")
        .WithName("GetWeddings")
        .RequireAuthorization()
        .Produces<IEnumerable<WeddingWithRoleDto>>(200)
        .Produces<ErrorResponse>(401);
    }
}
