using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class RemoveGuestFromEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/events/{eventId}/guests/{guestId}",
                async (Guid eventId, Guid guestId, IEventService eventService) =>
                {
                    var result = await eventService.RemoveGuestFromEventAsync(eventId, guestId);
                    return result switch
                    {
                        EventGuestChangeResult.Removed => Results.NoContent(),
                        EventGuestChangeResult.NotInEvent => Results.NotFound(new { error = "Guest not in event" }),
                        EventGuestChangeResult.NotFound => Results.NotFound(new { error = "Event not found" }),
                        EventGuestChangeResult.Unauthorized => Results.Forbid(),
                        _ => Results.BadRequest()
                    };
                })
            .WithTags("Events")
            .WithName("RemoveGuestFromEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
