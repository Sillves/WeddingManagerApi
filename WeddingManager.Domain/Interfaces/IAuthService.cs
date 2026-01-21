namespace WeddingManager.Domain.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, Guid? UserId)> RegisterAsync(string email, string firstName, string lastName, string password);
    Task<(bool Success, string Message, string? Token)> LoginAsync(string email, string password);
}
