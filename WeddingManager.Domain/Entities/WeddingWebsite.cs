using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class WeddingWebsite
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;

    public WebsiteTemplate Template { get; set; } = WebsiteTemplate.ElegantClassic;
    public string Settings { get; set; } = "{}";
    public string Content { get; set; } = "{}";

    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }
    public string? MetaDescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
