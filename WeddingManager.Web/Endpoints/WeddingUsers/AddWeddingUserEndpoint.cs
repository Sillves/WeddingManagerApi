using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class AddWeddingUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/users", 
            async (Guid weddingId, AddWeddingUserRequestDto requestDto, IWeddingUserService weddingUserService) =>
            {
                var result = await weddingUserService.AddUserToWeddingAsync(weddingId, requestDto);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                var weddingUser = result.Value!;
                return Results.Created($"/api/weddings/{weddingId}/users/{weddingUser.UserId}", weddingUser);
            })
            .WithTags("WeddingUsers")
            .WithName("AddWeddingUser")
            .RequireAuthorization()
            .Produces<WeddingUserDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403);
    }
}
