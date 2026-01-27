using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class UpdateEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/events/{eventId}",
                async (Guid eventId, UpdateEventRequestDto requestDto, IEventService eventService) =>
                {
                    try
                    {
                        var @event = await eventService.UpdateEventAsync(eventId, requestDto);
                        return Results.Ok(@event);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.NotFound(new { error = ex.Message });
                    }
                })
            .WithTags("Events")
            .WithName("UpdateEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces<EventDto>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
