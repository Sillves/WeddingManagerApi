using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class WeddingUser
{
    public Guid WeddingId { get; set; }
    public Guid UserId { get; set; }
    public WeddingUserRole Role { get; set; } = WeddingUserRole.Owner;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Wedding Wedding { get; set; } = null!;
    public User User { get; set; } = null!;
}
