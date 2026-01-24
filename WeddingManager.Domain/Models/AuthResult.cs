namespace WeddingManager.Domain.Models;

public class AuthResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Token { get; init; }
}
