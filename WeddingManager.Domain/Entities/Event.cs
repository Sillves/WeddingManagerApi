namespace WeddingManager.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public required string Location { get; set; }
    public string? Description { get; set; }

    public ICollection<Guest> Guests { get; set; } = null!;

    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
}
