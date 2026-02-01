using System.Text.Json;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class PublicWeddingWebsiteDto
{
    public string WeddingSlug { get; set; } = string.Empty;
    public string CoupleNames { get; set; } = string.Empty;
    public DateTime WeddingDate { get; set; }
    public string WeddingLocation { get; set; } = string.Empty;
    public WebsiteTemplate Template { get; set; }
    public JsonDocument Settings { get; set; } = null!;
    public JsonDocument Content { get; set; } = null!;
    public List<EventDto>? Events { get; set; }
}
