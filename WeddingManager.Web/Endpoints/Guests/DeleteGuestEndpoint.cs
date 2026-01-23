using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Guests;

public class DeleteGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/guests/{guestId}", 
            async (Guid guestId, IGuestService guestService) =>
            {
                try
                {
                    await guestService.DeleteGuestAsync(guestId);
                    return Results.NoContent();
                }
                catch (ArgumentException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .WithTags("Guests")
            .WithName("DeleteGuest")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<RequireGuestAccessFilter>()
            .Produces(204)
            .Produces(403)
            .Produces(404);
    }
}
