using Microsoft.AspNetCore.Identity;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? ReferralCode { get; set; }

    public ICollection<Wedding> Weddings { get; set; } = null!;
    public ICollection<WeddingUser> WeddingUsers { get; set; } = null!;
}
