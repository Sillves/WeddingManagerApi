using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.Guests;

public class UpdateGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/guests/{guestId}", 
            async (Guid guestId, UpdateGuestRequestDto requestDto, IGuestService guestService) =>
            {
                try
                {
                    var guest = await guestService.UpdateGuestAsync(guestId, requestDto);
                    return Results.Ok(guest);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .WithTags("Guests")
            .WithName("UpdateGuest")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces(200)
            .Produces(400)
            .Produces(403)
            .Produces(404);
    }
}
