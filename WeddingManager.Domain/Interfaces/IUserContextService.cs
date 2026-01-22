namespace WeddingManager.Domain.Interfaces;

public interface IUserContextService
{
    Guid GetUserId();
    string GetUserEmail();
    bool IsAuthenticated { get; }
}