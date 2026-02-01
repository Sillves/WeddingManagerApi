using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class CreateWedding : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings", async (CreateWeddingRequestDto requestDto, IWeddingService weddingService, ApplicationMapper mapper) =>
        {
            var wedding = mapper.CreateWeddingRequestToWedding(requestDto);

            var result = await weddingService.AddAsync(wedding);
            if (!result.IsSuccess)
            {
                return result.ToErrorResult();
            }

            return Results.Created($"/api/weddings/{wedding.Id}", wedding);
        })
        .WithTags("Weddings")
        .WithName("CreateWedding")
        .RequireAuthorization()
        .Produces<Wedding>(201)
        .Produces<ErrorResponse>(401);
    }
}
