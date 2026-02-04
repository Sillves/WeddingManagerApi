namespace WeddingManager.Domain.DTO;

public class EventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<GuestDto> GuestDtos { get; set; } = new();
    public Guid WeddingId { get; set; }
}
