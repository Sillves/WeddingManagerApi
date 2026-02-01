namespace WeddingManager.Domain.Entities;

public class Media
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string S3Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
}