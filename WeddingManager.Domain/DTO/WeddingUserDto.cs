using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class WeddingUserDto
{
    public Guid WeddingId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public WeddingUserRole Role { get; set; }
    public DateTime AddedAt { get; set; }
    public bool CanAccessGuests { get; set; }
    public bool CanAccessEvents { get; set; }
    public bool CanAccessExpenses { get; set; }
    public bool CanAccessWebsite { get; set; }
    public bool IsReadOnly { get; set; }
}
