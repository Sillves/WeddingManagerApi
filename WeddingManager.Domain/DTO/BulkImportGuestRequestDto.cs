using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class BulkImportGuestRequestDto
{
    public List<BulkImportGuestItemDto> Guests { get; set; } = [];
}

public class BulkImportGuestItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.Pending;
    public string? PreferredLanguage { get; set; }
}
