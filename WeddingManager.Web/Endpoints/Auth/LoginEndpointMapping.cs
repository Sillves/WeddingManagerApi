using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Models;
using WeddingManager.Web.Extensions;

namespace WeddingManager.Web.Endpoints.Auth;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request.Email, request.Password);

            if (!result.IsSuccess)
                return result.ToErrorResult();

            return Results.Ok(result.Value);
        })
        .WithTags("Auth")
        .WithName("Login")
        .RequireRateLimiting("AuthEndpoint")
        .Produces<AuthResult>(200)
        .Produces<ErrorResponse>(401);
    }
}
