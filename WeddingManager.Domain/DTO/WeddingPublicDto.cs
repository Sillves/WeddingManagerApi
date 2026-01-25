namespace WeddingManager.Domain.DTO;

public class WeddingPublicDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Location { get; set; } = null!;
}
