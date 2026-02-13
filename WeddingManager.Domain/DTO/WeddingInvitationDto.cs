using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class WeddingInvitationDto
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public string Email { get; set; } = string.Empty;
    public WeddingUserRole Role { get; set; }
    public bool CanAccessGuests { get; set; }
    public bool CanAccessEvents { get; set; }
    public bool CanAccessExpenses { get; set; }
    public bool CanAccessWebsite { get; set; }
    public bool IsReadOnly { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
