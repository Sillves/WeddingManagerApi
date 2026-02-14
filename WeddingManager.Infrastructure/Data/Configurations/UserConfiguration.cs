using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.SubscriptionTier).HasDefaultValue(SubscriptionTier.Free);
        builder.Property(e => e.StripeCustomerId).HasMaxLength(255);
        builder.Property(e => e.StripeSubscriptionId).HasMaxLength(255);
        builder.Property(u => u.ReferralCode).HasMaxLength(100);
        builder.HasIndex(u => u.ReferralCode).IsUnique().HasFilter("\"ReferralCode\" IS NOT NULL");
    }
}
