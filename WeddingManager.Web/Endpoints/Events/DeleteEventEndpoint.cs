using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class DeleteEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/events/{eventId}",
                async (Guid eventId, IEventService eventService) =>
                {
                    try
                    {
                        await eventService.DeleteEventAsync(eventId);
                        return Results.NoContent();
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return Results.NotFound(new { error = ex.Message });
                    }
                })
            .WithTags("Events")
            .WithName("DeleteEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces(204)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
