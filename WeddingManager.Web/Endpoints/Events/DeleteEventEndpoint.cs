using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class DeleteEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/events/{eventId}",
                async (Guid eventId, IEventService eventService) =>
                {
                    var result = await eventService.DeleteEventAsync(eventId);
                    return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
                })
            .WithTags("Events")
            .WithName("DeleteEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
