using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class GetWeddings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings", async (IWeddingService weddingService, ApplicationMapper mapper) =>
        {
            var result = await weddingService.GetAllAsync();
            if (!result.IsSuccess)
            {
                return result.ToErrorResult();
            }

            var dtos = mapper.WeddingsToDto(result.Value!);
            return Results.Ok(dtos);
        })
        .WithTags("Weddings")
        .WithName("GetWeddings")
        .RequireAuthorization()
        .Produces<IEnumerable<WeddingDto>>(200)
        .Produces<ErrorResponse>(401);
    }
}
