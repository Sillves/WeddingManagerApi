using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(
                request.Email, 
                request.FirstName, 
                request.LastName, 
                request.Password);

            if (!result.Success)
                return Results.BadRequest(new AuthResponse(false, result.Message));

            return Results.Created($"/api/users/{result.UserId}", 
                new AuthResponse(true, result.Message, UserId: result.UserId));
        })
        .WithTags("Auth")
        .WithName("Register")
        .WithOpenApi();
    }
}
