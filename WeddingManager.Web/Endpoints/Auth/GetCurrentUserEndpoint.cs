using Microsoft.AspNetCore.Identity;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", async (UserManager<User> userManager, IUserContextService userContextService) =>
            {
                var userId = userContextService.GetUserId();
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    var result = Result<UserProfileDto>.Fail(new Error(ErrorCodes.NotFound, "User not found."));
                    return result.ToErrorResult();
                }

                var profile = new UserProfileDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FirstName,
                    user.LastName,
                    user.SubscriptionTier,
                    user.AccountType);

                return Results.Ok(profile);
            })
            .WithTags("Auth")
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .Produces<UserProfileDto>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(404);
    }
}
