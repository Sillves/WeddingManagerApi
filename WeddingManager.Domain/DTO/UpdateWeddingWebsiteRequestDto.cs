using System.Text.Json;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class UpdateWeddingWebsiteRequestDto
{
    public WebsiteTemplate? Template { get; set; }
    public JsonDocument? Settings { get; set; }
    public JsonDocument? Content { get; set; }
    public string? MetaDescription { get; set; }
}
