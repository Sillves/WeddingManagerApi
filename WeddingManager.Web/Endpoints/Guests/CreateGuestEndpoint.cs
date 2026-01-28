using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Exceptions;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Guests;

public class CreateGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests", 
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
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
                catch (SubscriptionLimitExceededException ex)
                {
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithTags("Guests")
            .WithName("CreateGuest")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<GuestDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403);
    }
}
