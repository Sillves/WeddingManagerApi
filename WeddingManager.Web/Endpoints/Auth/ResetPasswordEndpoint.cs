using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/reset-password", async (ResetPasswordRequest request, IAuthService authService) =>
        {
            var result = await authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

            if (!result.IsSuccess)
                return result.ToErrorResult();

            return Results.Ok(new { message = "Password has been reset successfully." });
        })
        .WithTags("Auth")
        .WithName("ResetPassword")
        .Produces(200)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(404);
    }
}
