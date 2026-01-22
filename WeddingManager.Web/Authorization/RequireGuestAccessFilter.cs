using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Authorization;

public class RequireGuestAccessFilter(
    IGuestRepository guestRepository,
    IWeddingRepository weddingRepository,
    IUserContextService userContextService) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.RouteValues.TryGetValue("guestId", out var guestIdObj) ||
            !Guid.TryParse(guestIdObj?.ToString(), out var guestId))
        {
            return Results.BadRequest(new { error = "Invalid guest ID" });
        }

        var guest = await guestRepository.GetByIdAsync(guestId);
        if (guest == null)
        {
            return Results.NotFound(new { error = "Guest not found" });
        }

        var wedding = await weddingRepository.GetByIdAsync(guest.WeddingId);
        var userId = userContextService.GetUserId();

        if (wedding?.UserId != userId)
        {
            return Results.Forbid();
        }

        context.HttpContext.Items["Guest"] = guest;
        context.HttpContext.Items["Wedding"] = wedding;
        return await next(context);
    }
}
