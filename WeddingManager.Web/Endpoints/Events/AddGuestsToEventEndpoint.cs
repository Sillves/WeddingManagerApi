using Microsoft.AspNetCore.Mvc;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Events;

public class AddGuestsToEventEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{eventId}/guests",
                async (Guid eventId, [FromBody] AddGuestsToEventRequestDto? requestDto, IEventService eventService) =>
                {
                    if (requestDto?.GuestIds.Count is null or 0)
                    {
                        return Result.Fail(new Error(ErrorCodes.Validation, "GuestIds are required"))
                            .ToErrorResult();
                    }

                    var result = await eventService.AddGuestsToEventAsync(eventId, requestDto.GuestIds);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Events")
            .WithName("AddGuestsToEvent")
            .RequireAuthorization()
            .AddEndpointFilter<RequireEventAccessFilter>()
            .RequireModuleAccess(WeddingModule.Events)
            .Produces<EventGuestBatchChangeResultDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
