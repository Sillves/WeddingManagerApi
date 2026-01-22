
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class AddWeddingUserRequestDto
{
    public Guid UserId { get; set; }
    public WeddingUserRole Role { get; set; } = WeddingUserRole.Planner;
}
