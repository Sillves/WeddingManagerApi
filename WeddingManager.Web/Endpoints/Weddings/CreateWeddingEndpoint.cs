using AutoMapper;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class CreateWedding : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/weddings", async (CreateWeddingRequestDto requestDto, IWeddingService weddingService, IMapper mapper) =>
        {
            var wedding = mapper.Map<Wedding>(requestDto);

            await weddingService.AddAsync(wedding);
            return Results.Created($"/api/weddings/{wedding.Id}", wedding);
        })
        .WithTags("Weddings")
        .WithName("CreateWedding")
        .WithOpenApi()
        .RequireAuthorization();
    }
}
