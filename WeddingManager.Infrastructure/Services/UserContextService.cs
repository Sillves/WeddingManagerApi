using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Infrastructure.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) 
                          ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new InvalidOperationException("User ID claim not found or invalid.");
        }
        return userId;
    }

    public string GetUserEmail()
    {
        var emailClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email) 
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst("email");
        
        return emailClaim == null ? throw new InvalidOperationException("User email claim not found.") : emailClaim.Value;
    }
    
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}