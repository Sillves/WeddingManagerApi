namespace WeddingManager.Domain.DTO;

/// <summary>
/// Initial public state for a wedding's RSVP. If exactly one open (no-passcode) flow exists,
/// <see cref="Flow"/> is populated directly; otherwise the guest must unlock with a passcode.
/// </summary>
public class RsvpFlowStateDto
{
    public bool HasFlows { get; set; }
    public bool RequiresPasscode { get; set; }
    public RsvpFlowPublicDto? Flow { get; set; }
}
