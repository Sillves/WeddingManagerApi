using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Web.Authorization;

public static class EndpointAuthorizationExtensions
{
    public static RouteHandlerBuilder RequireOwnerAccess(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var weddingUser = context.HttpContext.Items["WeddingUser"] as WeddingUser;
            if (weddingUser is not { Role: WeddingUserRole.Owner })
            {
                return Results.Forbid();
            }

            return await next(context);
        });
    }

    public static RouteHandlerBuilder RequireModuleAccess(this RouteHandlerBuilder builder, WeddingModule module)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var weddingUser = context.HttpContext.Items["WeddingUser"] as WeddingUser;
            if (weddingUser == null)
            {
                return Results.Forbid();
            }

            if (weddingUser.Role == WeddingUserRole.Owner)
            {
                return await next(context);
            }

            var hasAccess = module switch
            {
                WeddingModule.Guests => weddingUser.CanAccessGuests,
                WeddingModule.Events => weddingUser.CanAccessEvents,
                WeddingModule.Expenses => weddingUser.CanAccessExpenses,
                WeddingModule.Website => weddingUser.CanAccessWebsite,
                _ => false
            };

            if (!hasAccess)
            {
                return Results.Forbid();
            }

            if (weddingUser.IsReadOnly && context.HttpContext.Request.Method != "GET")
            {
                return Results.Forbid();
            }

            return await next(context);
        });
    }
}
