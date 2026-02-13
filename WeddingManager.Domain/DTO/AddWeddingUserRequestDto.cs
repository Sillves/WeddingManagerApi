
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class AddWeddingUserRequestDto
{
    public Guid UserId { get; set; }
    public WeddingUserRole Role { get; set; } = WeddingUserRole.Planner;
    public bool CanAccessGuests { get; set; } = true;
    public bool CanAccessEvents { get; set; } = true;
    public bool CanAccessExpenses { get; set; } = true;
    public bool CanAccessWebsite { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
}
