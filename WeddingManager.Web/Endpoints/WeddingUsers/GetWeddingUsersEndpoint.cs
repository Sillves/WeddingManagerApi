using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Endpoints;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class GetWeddingUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/weddings/{weddingId}/users", 
            async (Guid weddingId, IWeddingUserService weddingUserService) =>
            {
                try
                {
                    var users = await weddingUserService.GetWeddingUsersAsync(weddingId);
                    return Results.Ok(users);
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
            .WithName("GetWeddingUsers")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces(200)
            .Produces(403)
            .Produces(404);
    }
}
