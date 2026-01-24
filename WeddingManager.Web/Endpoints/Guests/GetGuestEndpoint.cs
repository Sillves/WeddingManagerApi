
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Guests;

public class GetGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/guests/{guestId}", 
            async (Guid guestId, IGuestService guestService) =>
            {
                try
                {
                    var guest = await guestService.GetByIdAsync(guestId);
                    return guest == null 
                        ? Results.NotFound(new { error = "Guest not found" })
                        : Results.Ok(guest);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .WithTags("Guests")
            .WithName("GetGuest")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<RequireGuestAccessFilter>()
            .Produces(200)
            .Produces(403)
            .Produces(404);
    }
}
