using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class UpdateWeddingUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/weddings/{weddingId}/users/{userId}",
            async (Guid weddingId, Guid userId, UpdateWeddingUserRequestDto requestDto, IWeddingUserService weddingUserService) =>
            {
                var result = await weddingUserService.UpdatePermissionsAsync(weddingId, userId, requestDto);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("WeddingUsers")
            .WithName("UpdateWeddingUser")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireOwnerAccess()
            .Produces<WeddingUserDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
