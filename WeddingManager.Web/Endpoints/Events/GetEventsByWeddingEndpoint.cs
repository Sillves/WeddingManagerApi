using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class GetEventsByWeddingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/events",
                async (Guid weddingId, IEventService eventService) =>
                {
                    var events = await eventService.GetByWeddingIdAsync(weddingId);
                    return Results.Ok(events);
                })
            .WithTags("Events")
            .WithName("GetEventsByWedding")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<IEnumerable<EventDto>>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
