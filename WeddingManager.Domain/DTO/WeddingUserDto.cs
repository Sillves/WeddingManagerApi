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
}
