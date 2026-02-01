namespace WeddingManager.Domain.DTO;

public class UpdateWeddingRequestDto
{
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public string? Location { get; set; }
}
