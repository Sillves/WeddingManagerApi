using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class RemoveWeddingUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/weddings/{weddingId}/users/{userId}", 
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
            .RequireAuthorization()
            .Produces(204)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
