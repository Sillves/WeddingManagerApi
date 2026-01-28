namespace WeddingManager.Domain.Entities;

public class SubscriptionUsage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int EmailsSent { get; set; }
}
