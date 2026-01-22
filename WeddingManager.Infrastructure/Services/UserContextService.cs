using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Infrastructure.Services;

public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    public Guid GetUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) 
                          ?? httpContextAccessor.HttpContext?.User.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new InvalidOperationException("User ID claim not found or invalid.");
        }
        return userId;
    }

    public string GetUserEmail()
    {
        var emailClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email) 
                         ?? httpContextAccessor.HttpContext?.User.FindFirst("email");
        
        return emailClaim == null ? throw new InvalidOperationException("User email claim not found.") : emailClaim.Value;
    }
    
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}