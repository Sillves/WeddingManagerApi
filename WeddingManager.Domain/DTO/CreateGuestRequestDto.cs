using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class CreateGuestRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.Pending;
}
