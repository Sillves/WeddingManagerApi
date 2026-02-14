using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class Referral
{
    public Guid Id { get; set; }
    public Guid ReferrerUserId { get; set; }
    public Guid? ReferredUserId { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public DateTime? RegisteredAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public decimal CommissionPercentage { get; set; } = 15.0m;
    public CommissionStatus CommissionStatus { get; set; } = CommissionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User ReferrerUser { get; set; } = null!;
    public User? ReferredUser { get; set; }
}
