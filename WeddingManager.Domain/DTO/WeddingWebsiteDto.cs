using System.Text.Json;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class WeddingWebsiteDto
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public string WeddingSlug { get; set; } = string.Empty;
    public WebsiteTemplate Template { get; set; }
    public JsonDocument Settings { get; set; } = null!;
    public JsonDocument Content { get; set; } = null!;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? MetaDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
