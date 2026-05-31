using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.Entities;

public class InvitationFlow
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;

    /// <summary>Planner-facing label only ("Family", "Day-only"). Never shown to guests.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Plain text. Null = open flow (only one allowed per wedding). Unique within wedding when set.</summary>
    public string? Passcode { get; set; }

    public bool IncludePlusOne { get; set; }

    public List<QuestionDefinition> CustomQuestions { get; set; } = new();

    /// <summary>Ids of real <see cref="Event"/> rows this flow exposes for selection.</summary>
    public List<Guid> EventIds { get; set; } = new();

    /// <summary>Flow-local events not backed by an <see cref="Event"/> row.</summary>
    public List<CustomEventDefinition> CustomEvents { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RsvpResponse> Responses { get; set; } = null!;
}
