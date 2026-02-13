using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Authorization;

public class RequireGuestAccessFilter(
    IGuestRepository guestRepository,
    IWeddingRepository weddingRepository,
    IWeddingUserRepository weddingUserRepository,
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
        if (wedding == null)
        {
            return Results.Forbid();
        }

        var userId = userContextService.GetUserId();
        var weddingUser = await weddingUserRepository.GetByIdAsync(guest.WeddingId, userId);
        if (weddingUser == null)
        {
            return Results.Forbid();
        }

        context.HttpContext.Items["Guest"] = guest;
        context.HttpContext.Items["Wedding"] = wedding;
        context.HttpContext.Items["WeddingUser"] = weddingUser;
        return await next(context);
    }
}
