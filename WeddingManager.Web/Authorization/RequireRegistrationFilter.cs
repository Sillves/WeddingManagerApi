using System.Net;
using WeddingManager.Domain.Models;

namespace WeddingManager.Web.Authorization;

public class RequireRegistrationFilter(
    IConfiguration configuration,
    IWebHostEnvironment environment) : IEndpointFilter
{
    private const string RegistrationCodeHeader = "X-Registration-Code";

    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var registrationEnabled = configuration.GetValue<bool?>("Registration:Enabled") ?? environment.IsDevelopment();
        if (!registrationEnabled)
        {
            return ValueTask.FromResult<object?>(
                Results.Json(
                    new AuthResult { Success = false, Message = "Registration is disabled", Token = null },
                    statusCode: (int)HttpStatusCode.Forbidden));
        }

        var inviteCode = configuration["Registration:InviteCode"];
        if (!string.IsNullOrWhiteSpace(inviteCode))
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(RegistrationCodeHeader, out var providedCode)
                || !string.Equals(providedCode.ToString(), inviteCode, StringComparison.Ordinal))
            {
                return ValueTask.FromResult<object?>(
                    Results.Json(
                        new AuthResult { Success = false, Message = "Invalid registration code", Token = null },
                        statusCode: (int)HttpStatusCode.Forbidden));
            }
        }

        return next(context);
    }
}
