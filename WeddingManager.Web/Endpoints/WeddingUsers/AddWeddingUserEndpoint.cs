using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class AddWeddingUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/users", 
            async (Guid weddingId, AddWeddingUserRequestDto requestDto, IWeddingUserService weddingUserService) =>
            {
                try
                {
                    var weddingUser = await weddingUserService.AddUserToWeddingAsync(weddingId, requestDto);
                    return Results.Created($"/api/weddings/{weddingId}/users/{weddingUser.UserId}", weddingUser);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Forbid();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithTags("WeddingUsers")
            .WithName("AddWeddingUser")
            .RequireAuthorization()
            .Produces<WeddingUserDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403);
    }
}
