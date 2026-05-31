using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class GuestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Dietary { get; set; }
    public RsvpStatus RsvpStatus { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public DateTime? InvitationSentAt { get; set; }
    public Guid WeddingId { get; set; }
    public Guid? PlusOneOfGuestId { get; set; }
}
