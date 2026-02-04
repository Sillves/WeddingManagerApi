namespace WeddingManager.Domain.DTO;

public class UpdateEventRequestDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
}
