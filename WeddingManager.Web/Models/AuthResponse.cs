namespace WeddingManager.Web.Models;

public record AuthResponse(bool Success, string Message, string? Token = null, Guid? UserId = null);