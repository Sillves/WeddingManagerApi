using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class AddGuestToEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{eventId}/guests/{guestId}",
                async (Guid eventId, Guid guestId, IEventService eventService) =>
                {
                    var result = await eventService.AddGuestToEventAsync(eventId, guestId);
                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    return Results.Ok(result.Value);
                })
            .WithTags("Events")
            .WithName("AddGuestToEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(409);
    }
}
