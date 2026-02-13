using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class CreateEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/events",
                async (Guid weddingId, CreateEventRequestDto requestDto, IEventService eventService) =>
                {
                    var result = await eventService.CreateEventAsync(weddingId, requestDto);
                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    var @event = result.Value!;
                    return Results.Created($"/api/events/{@event.Id}", @event);
                })
            .WithTags("Events")
            .WithName("CreateEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Events)
            .Produces<EventDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(500);
    }
}
