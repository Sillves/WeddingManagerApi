using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class RemoveGuestFromEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/events/{eventId}/guests/{guestId}",
                async (Guid eventId, Guid guestId, IEventService eventService) =>
                {
                    var result = await eventService.RemoveGuestFromEventAsync(eventId, guestId);
                    return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
                })
            .WithTags("Events")
            .WithName("RemoveGuestFromEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces(204)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
