namespace WeddingManager.Domain.Models;

/// <summary>
/// A flow-local event (not backed by a real <see cref="Entities.Event"/> row), stored as jsonb.
/// Its <see cref="Id"/> can appear in <see cref="Entities.RsvpResponse.AttendingEventIds"/>.
/// </summary>
public class CustomEventDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public string? Location { get; set; }
}
