using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Endpoints;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class RemoveWeddingUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/weddings/{weddingId}/users/{userId}", 
            async (Guid weddingId, Guid userId, IWeddingUserService weddingUserService) =>
            {
                try
                {
                    await weddingUserService.RemoveUserFromWeddingAsync(weddingId, userId);
                    return Results.NoContent();
                }
                catch (ArgumentException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
            })
            .WithTags("WeddingUsers")
            .WithName("RemoveWeddingUser")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces(204)
            .Produces(403)
            .Produces(404);
    }
}
