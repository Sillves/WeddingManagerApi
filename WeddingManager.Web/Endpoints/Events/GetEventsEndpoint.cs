using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.Events;

public class GetEventsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/events",
                async (IEventService eventService) =>
                {
                    var events = await eventService.GetAllAsync();
                    return Results.Ok(events);
                })
            .WithTags("Events")
            .WithName("GetEvents")
            .RequireAuthorization()
            .Produces<IEnumerable<EventDto>>(200)
            .Produces(401);
    }
}
