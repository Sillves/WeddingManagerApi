using System.Net;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Auth;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, IAuthService authService, IConfiguration configuration, IWebHostEnvironment environment, HttpRequest httpRequest) =>
        {
            var registrationEnabled = configuration.GetValue<bool?>("Registration:Enabled") ?? environment.IsDevelopment();
            if (!registrationEnabled)
            {
                return Results.Json(
                    new AuthResult { Success = false, Message = "Registration is disabled", Token = null },
                    statusCode: (int)HttpStatusCode.Forbidden);
            }

            var inviteCode = configuration["Registration:InviteCode"];
            if (!string.IsNullOrWhiteSpace(inviteCode))
            {
                if (!httpRequest.Headers.TryGetValue("X-Registration-Code", out var providedCode)
                    || !string.Equals(providedCode.ToString(), inviteCode, StringComparison.Ordinal))
                {
                    return Results.Json(
                        new AuthResult { Success = false, Message = "Invalid registration code", Token = null },
                        statusCode: (int)HttpStatusCode.Forbidden);
                }
            }

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
        .Produces<AuthResult>((int)HttpStatusCode.Forbidden)
        .Produces<AuthResult>((int)HttpStatusCode.BadRequest);
    }
}
