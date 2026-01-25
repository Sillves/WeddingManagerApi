using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request.Email, request.Password);

            if (!result.Success)
                return Results.Unauthorized();

            return Results.Ok(result);
        })
        .WithTags("Auth")
        .WithName("Login")
        .Produces<AuthResult>(200)
        .Produces(401);
    }
}
