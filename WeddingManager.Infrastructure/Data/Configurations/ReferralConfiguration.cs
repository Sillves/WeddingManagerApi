using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReferralCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.CommissionPercentage)
            .HasPrecision(5, 2);

        builder.Property(r => r.CommissionStatus)
            .IsRequired()
            .HasConversion<string>();

        builder.HasOne(r => r.ReferrerUser)
            .WithMany()
            .HasForeignKey(r => r.ReferrerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReferredUser)
            .WithMany()
            .HasForeignKey(r => r.ReferredUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.ReferralCode);
        builder.HasIndex(r => r.ReferredUserId);
    }
}
