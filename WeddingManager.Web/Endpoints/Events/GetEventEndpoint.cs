using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class GetEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/events/{eventId}",
                async (Guid eventId, IEventService eventService) =>
                {
                    var @event = await eventService.GetByIdAsync(eventId);
                    return @event == null
                        ? Results.NotFound(new { error = "Event not found" })
                        : Results.Ok(@event);
                })
            .WithTags("Events")
            .WithName("GetEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces<EventDto>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
