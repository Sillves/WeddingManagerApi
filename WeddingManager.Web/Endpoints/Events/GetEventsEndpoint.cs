using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class GetEventsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/events",
                async (IEventService eventService) =>
                {
                    var result = await eventService.GetAllAsync();
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Events")
            .WithName("GetEvents")
            .RequireAuthorization()
            .Produces<IEnumerable<EventDto>>(200)
            .Produces<ErrorResponse>(401);
    }
}
