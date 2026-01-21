using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class CreateWedding : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/weddings", async (CreateWeddingRequest request, IWeddingService weddingService) =>
        {
            var wedding = new Wedding
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Slug = request.Slug,
                Date = request.Date,
                Location = request.Location,
                UserId = request.UserId
            };

            await weddingService.AddAsync(wedding);
            return Results.Created($"/api/weddings/{wedding.Id}", wedding);
        })
        .WithTags("Weddings")
        .WithName("CreateWedding")
        .WithOpenApi();
    }
}
