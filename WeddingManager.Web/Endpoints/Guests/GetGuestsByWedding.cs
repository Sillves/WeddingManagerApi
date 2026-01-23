using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Endpoints;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Guests;

public class GetGuestsByWedding : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/weddings/{weddingId}/guests", 
            async (Guid weddingId, IGuestService guestService) =>
            {
                var guests = await guestService.GetByWeddingIdAsync(weddingId);
                return Results.Ok(guests);
            })
            .WithTags("Guests")
            .WithName("GetGuestsByWedding")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces(200)
            .Produces(403)
            .Produces(404);
    }
}
