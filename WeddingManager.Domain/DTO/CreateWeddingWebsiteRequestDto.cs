using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class CreateWeddingWebsiteRequestDto
{
    public WebsiteTemplate Template { get; set; } = WebsiteTemplate.ElegantClassic;
}
