using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string firstName, string lastName, string password);
    Task<AuthResult> LoginAsync(string email, string password);
}
