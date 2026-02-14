using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResult>> RegisterAsync(string email, string firstName, string lastName, string password, string? referralCode = null);
    Task<Result<AuthResult>> LoginAsync(string email, string password);
    Task<Result> RequestPasswordResetAsync(string email, string language = "en");
    Task<Result> ResetPasswordAsync(string email, string token, string newPassword);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
