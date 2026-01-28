using Microsoft.AspNetCore.Mvc;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class RemoveGuestsFromEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/events/{eventId}/guests",
                async (Guid eventId, [FromBody] RemoveGuestsFromEventRequestDto? requestDto, IEventService eventService) =>
                {
                    if (requestDto?.GuestIds.Count is null or 0)
                    {
                        return Results.BadRequest(new { error = "GuestIds are required" });
                    }

                    var result = await eventService.RemoveGuestsFromEventAsync(eventId, requestDto.GuestIds);
                    return result.Status switch
                    {
                        EventGuestChangeResult.NotFound => Results.NotFound(new { error = "Event not found" }),
                        EventGuestChangeResult.Unauthorized => Results.Forbid(),
                        _ => Results.Ok(result)
                    };
                })
            .WithTags("Events")
            .WithName("RemoveGuestsFromEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces<EventGuestBatchRemoveResultDto>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
