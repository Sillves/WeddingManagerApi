using System.Net;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
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

            if (!result.IsSuccess)
                return result.ToErrorResult();

            return Results.Created(string.Empty, result.Value);
        })
        // .AddEndpointFilter<RequireRegistrationFilter>()
        .WithTags("Auth")
        .WithName("Register")
        .RequireRateLimiting("AuthEndpoint")
        .Produces<AuthResult>((int)HttpStatusCode.Created)
        .Produces<ErrorResponse>((int)HttpStatusCode.Forbidden)
        .Produces<ErrorResponse>((int)HttpStatusCode.BadRequest);
    }
}
