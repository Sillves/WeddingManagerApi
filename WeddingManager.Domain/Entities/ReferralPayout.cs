using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class ReferralPayout
{
    public Guid Id { get; set; }
    public Guid ReferrerUserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? PaidAt { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    public User ReferrerUser { get; set; } = null!;
}
