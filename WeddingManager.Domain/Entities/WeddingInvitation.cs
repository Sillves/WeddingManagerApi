using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class WeddingInvitation
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public string Email { get; set; } = string.Empty;
    public WeddingUserRole Role { get; set; } = WeddingUserRole.Planner;
    public bool CanAccessGuests { get; set; } = true;
    public bool CanAccessEvents { get; set; } = true;
    public bool CanAccessExpenses { get; set; } = true;
    public bool CanAccessWebsite { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wedding Wedding { get; set; } = null!;
}
