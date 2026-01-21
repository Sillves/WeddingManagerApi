namespace WeddingManager.Web.Models;

public class CreateWeddingRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}
