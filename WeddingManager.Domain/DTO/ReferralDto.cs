using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class ReferralDto
{
    public Guid Id { get; set; }
    public string? ReferredUserName { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public decimal CommissionPercentage { get; set; }
    public CommissionStatus CommissionStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
