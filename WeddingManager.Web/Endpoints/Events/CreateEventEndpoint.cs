using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Exceptions;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;

namespace WeddingManager.Web.Endpoints.Events;

public class CreateEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/events",
                async (Guid weddingId, CreateEventRequestDto requestDto, IEventService eventService) =>
                {
                    try
                    {
                        var @event = await eventService.CreateEventAsync(weddingId, requestDto);
                        return Results.Created($"/api/events/{@event.Id}", @event);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }
                    catch (SubscriptionLimitExceededException ex)
                    {
                        return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
                    }
                })
            .WithTags("Events")
            .WithName("CreateEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<EventDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
