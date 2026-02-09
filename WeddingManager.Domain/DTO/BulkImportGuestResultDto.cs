namespace WeddingManager.Domain.DTO;

public class BulkImportGuestResultDto
{
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<GuestDto> CreatedGuests { get; set; } = [];
    public List<BulkImportGuestErrorDto> Errors { get; set; } = [];
}

public class BulkImportGuestErrorDto
{
    public int RowIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
