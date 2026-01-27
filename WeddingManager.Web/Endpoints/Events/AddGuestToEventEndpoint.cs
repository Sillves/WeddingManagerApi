using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class AddGuestToEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{eventId}/guests/{guestId}",
                async (Guid eventId, Guid guestId, IEventService eventService) =>
                {
                    var result = await eventService.AddGuestToEventAsync(eventId, guestId);
                    return result switch
                    {
                        EventGuestChangeResult.Added => Results.Ok(),
                        EventGuestChangeResult.AlreadyExists => Results.Conflict(new { error = "Guest already added to event" }),
                        EventGuestChangeResult.NotFound => Results.NotFound(new { error = "Event or guest not found" }),
                        EventGuestChangeResult.Unauthorized => Results.Forbid(),
                        _ => Results.BadRequest()
                    };
                })
            .WithTags("Events")
            .WithName("AddGuestToEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(409);
    }
}
