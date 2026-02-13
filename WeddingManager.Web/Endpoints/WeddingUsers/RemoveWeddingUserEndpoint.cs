using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class RemoveWeddingUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/weddings/{weddingId}/users/{userId}",
            async (Guid weddingId, Guid userId, IWeddingUserService weddingUserService) =>
            {
                var result = await weddingUserService.RemoveUserFromWeddingAsync(weddingId, userId);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
            .WithTags("WeddingUsers")
            .WithName("RemoveWeddingUser")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireOwnerAccess()
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
