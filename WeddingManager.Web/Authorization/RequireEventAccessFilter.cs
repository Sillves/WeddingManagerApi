using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Authorization;

public class RequireEventAccessFilter(
    IEventRepository eventRepository,
    IUserContextService userContextService) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.RouteValues.TryGetValue("eventId", out var eventIdObj) ||
            !Guid.TryParse(eventIdObj?.ToString(), out var eventId))
        {
            return Results.BadRequest(new { error = "Invalid event ID" });
        }

        var @event = await eventRepository.GetByIdAsync(eventId);
        if (@event == null)
        {
            return Results.NotFound(new { error = "Event not found" });
        }

        var userId = userContextService.GetUserId();
        if (@event.Wedding.UserId != userId)
        {
            return Results.Forbid();
        }

        context.HttpContext.Items["Event"] = @event;
        context.HttpContext.Items["Wedding"] = @event.Wedding;
        return await next(context);
    }
}
