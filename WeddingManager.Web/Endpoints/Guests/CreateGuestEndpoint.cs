using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.Guests;

public class CreateGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/weddings/{weddingId}/guests", 
            async (Guid weddingId, CreateGuestRequestDto requestDto, IGuestService guestService) =>
            {
                try
                {
                    var guest = await guestService.CreateGuestAsync(weddingId, requestDto);
                    return Results.Created($"/api/guests/{guest.Id}", guest);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Forbid();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithTags("Guests")
            .WithName("CreateGuest")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces(201)
            .Produces(400)
            .Produces(403);
    }
}
