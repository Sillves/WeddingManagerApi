using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class UpdateEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/events/{eventId}",
                async (Guid eventId, UpdateEventRequestDto requestDto, IEventService eventService) =>
                {
                    var result = await eventService.UpdateEventAsync(eventId, requestDto);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Events")
            .WithName("UpdateEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .Produces<EventDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
