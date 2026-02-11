using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class ForgotPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/forgot-password", async (ForgotPasswordRequest request, IAuthService authService) =>
        {
            var result = await authService.RequestPasswordResetAsync(request.Email, request.Language);

            if (!result.IsSuccess)
                return result.ToErrorResult();

            // Always return success to prevent email enumeration
            return Results.Ok(new { message = "If an account exists with this email, a password reset link has been sent." });
        })
        .WithTags("Auth")
        .WithName("ForgotPassword")
        .RequireRateLimiting("AuthEndpoint")
        .Produces(200)
        .Produces<ErrorResponse>(502);
    }
}
