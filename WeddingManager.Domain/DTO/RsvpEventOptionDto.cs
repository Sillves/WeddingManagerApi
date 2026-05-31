namespace WeddingManager.Domain.DTO;

/// <summary>An event a guest can pick in an RSVP flow — a real Event or a flow-local custom one.</summary>
public class RsvpEventOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public string? Location { get; set; }
}
