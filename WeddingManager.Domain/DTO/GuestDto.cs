using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class GuestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RsvpStatus RsvpStatus { get; set; }
    public Guid WeddingId { get; set; }
}
