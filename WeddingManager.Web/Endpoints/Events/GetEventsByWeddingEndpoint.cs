using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class GetEventsByWeddingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/events",
                async (Guid weddingId, IEventService eventService) =>
                {
                    var result = await eventService.GetByWeddingIdAsync(weddingId);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Events")
            .WithName("GetEventsByWedding")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<IEnumerable<EventDto>>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
