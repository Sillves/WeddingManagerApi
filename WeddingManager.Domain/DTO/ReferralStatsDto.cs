namespace WeddingManager.Domain.DTO;

public class ReferralStatsDto
{
    public string ReferralCode { get; set; } = string.Empty;
    public int SignedUpCount { get; set; }
    public int SubscribedCount { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal TotalCommissionEarned { get; set; }
    public decimal PendingPayout { get; set; }
}
