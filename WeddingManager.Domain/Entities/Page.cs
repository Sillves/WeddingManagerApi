namespace WeddingManager.Domain.Entities;

public class Page
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = "{}"; // JSON Content
    public int Order { get; set; }

    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
}