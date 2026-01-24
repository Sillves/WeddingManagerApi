using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Guests;

public class GetGuestsByWedding : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/guests", 
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
            .Produces<IEnumerable<GuestDto>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
