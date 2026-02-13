using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Authorization;

public class RequireWeddingAccessFilter(
    IWeddingRepository weddingRepository,
    IWeddingUserRepository weddingUserRepository,
    IUserContextService userContextService) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.RouteValues.TryGetValue("weddingId", out var weddingIdObj) ||
            !Guid.TryParse(weddingIdObj?.ToString(), out var weddingId))
        {
            return Results.BadRequest(new { error = "Invalid wedding ID" });
        }

        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding == null)
        {
            return Results.NotFound(new { error = "Wedding not found" });
        }

        var userId = userContextService.GetUserId();
        var weddingUser = await weddingUserRepository.GetByIdAsync(weddingId, userId);
        if (weddingUser == null)
        {
            return Results.Forbid();
        }

        context.HttpContext.Items["Wedding"] = wedding;
        context.HttpContext.Items["WeddingUser"] = weddingUser;
        return await next(context);
    }
}
