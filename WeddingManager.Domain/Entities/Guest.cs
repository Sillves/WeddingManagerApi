using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class Guest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Dietary { get; set; }
    public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.Pending;
    public string PreferredLanguage { get; set; } = "en";
    public string? InvitationToken { get; set; }
    public DateTime? InvitationTokenExpiresAt { get; set; }
    public DateTime? InvitationSentAt { get; set; }
    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
    public ICollection<Event> Events { get; set; } = null!;

    /// <summary>When set, this guest is a plus-one of another guest.</summary>
    public Guid? PlusOneOfGuestId { get; set; }
    public Guest? PlusOneOf { get; set; }
}
