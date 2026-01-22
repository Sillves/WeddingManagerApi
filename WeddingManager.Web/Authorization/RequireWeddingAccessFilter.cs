using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Authorization;

public class RequireWeddingAccessFilter(
    IWeddingRepository weddingRepository,
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
        if (wedding.UserId != userId)
        {
            return Results.Forbid();
        }

        context.HttpContext.Items["Wedding"] = wedding;
        return await next(context);
    }
}
