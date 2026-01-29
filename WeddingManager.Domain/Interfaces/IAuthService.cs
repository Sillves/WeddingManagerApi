using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResult>> RegisterAsync(string email, string firstName, string lastName, string password);
    Task<Result<AuthResult>> LoginAsync(string email, string password);
}
