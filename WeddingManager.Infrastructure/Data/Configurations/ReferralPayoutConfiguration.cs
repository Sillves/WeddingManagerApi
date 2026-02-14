using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class ReferralPayoutConfiguration : IEntityTypeConfiguration<ReferralPayout>
{
    public void Configure(EntityTypeBuilder<ReferralPayout> builder)
    {
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Amount)
            .HasPrecision(10, 2);

        builder.Property(rp => rp.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(rp => rp.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.HasOne(rp => rp.ReferrerUser)
            .WithMany()
            .HasForeignKey(rp => rp.ReferrerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rp => rp.ReferrerUserId);
    }
}
