using System.Net;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(
                request.Email, 
                request.FirstName, 
                request.LastName, 
                request.Password);

            if (!result.Success)
                return Results.BadRequest(result);

            return Results.Created(string.Empty, result);
        })
        .WithTags("Auth")
        .WithName("Register")
        .Produces<AuthResult>((int)HttpStatusCode.Created)
        .Produces<AuthResult>((int)HttpStatusCode.BadRequest);
    }
}
