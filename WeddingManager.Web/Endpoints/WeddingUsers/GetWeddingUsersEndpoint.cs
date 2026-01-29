using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.WeddingUsers;

public class GetWeddingUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/users", 
            async (Guid weddingId, IWeddingUserService weddingUserService) =>
            {
                var result = await weddingUserService.GetWeddingUsersAsync(weddingId);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("WeddingUsers")
            .WithName("GetWeddingUsers")
            .RequireAuthorization()
            .Produces<IEnumerable<WeddingUserDto>>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
