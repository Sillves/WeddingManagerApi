using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Endpoints;

namespace WeddingManager.Web.Endpoints.Weddings;

public class SubmitRsvpEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/weddings/{id}/rsvp",
            async (Guid id, RsvpSubmitRequestDto requestDto, IGuestService guestService) =>
            {
                try
                {
                    var guest = await guestService.SubmitRsvpAsync(id, requestDto);
                    return guest == null
                        ? Results.NotFound(new { error = "Guest not found for wedding" })
                        : Results.Ok(guest);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithTags("Weddings")
            .WithName("SubmitRsvp")
            .WithOpenApi()
            .Produces(200)
            .Produces(400)
            .Produces(404);
    }
}
