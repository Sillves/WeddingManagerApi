using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class WeddingWithRoleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public WeddingUserRole Role { get; set; }
    public int GuestCount { get; set; }
    public bool CanAccessGuests { get; set; }
    public bool CanAccessEvents { get; set; }
    public bool CanAccessExpenses { get; set; }
    public bool CanAccessWebsite { get; set; }
    public bool IsReadOnly { get; set; }
}
