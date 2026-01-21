namespace WeddingManager.Domain.Entities;

public class Guest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAttending { get; set; }

    public Guid WeddingId { get; set; }
    public Wedding Wedding { get; set; } = null!;
}