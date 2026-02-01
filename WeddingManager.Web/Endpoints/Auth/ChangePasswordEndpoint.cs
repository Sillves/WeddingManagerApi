using System.Security.Claims;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class ChangePasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/change-password", async (ChangePasswordRequest request, IAuthService authService, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

            if (!result.IsSuccess)
                return result.ToErrorResult();

            return Results.Ok(new { message = "Password changed successfully." });
        })
        .RequireAuthorization()
        .WithTags("Auth")
        .WithName("ChangePassword")
        .Produces(200)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(401)
        .Produces<ErrorResponse>(404);
    }
}
