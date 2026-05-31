using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class RsvpResponse
{
    public Guid Id { get; set; }

    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;

    public Guid InvitationFlowId { get; set; }
    public InvitationFlow InvitationFlow { get; set; } = null!;

    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public RsvpResponseStatus Status { get; set; }

    /// <summary>Subset of the flow's exposed events (real Event ids and/or flow-local custom-event ids).</summary>
    public List<Guid> AttendingEventIds { get; set; } = new();

    /// <summary>Raw jsonb object keyed by question id; heterogeneous values. Empty object for plus-ones.</summary>
    public string CustomAnswers { get; set; } = "{}";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
