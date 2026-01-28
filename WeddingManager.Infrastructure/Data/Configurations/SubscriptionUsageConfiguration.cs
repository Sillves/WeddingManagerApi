using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class SubscriptionUsageConfiguration : IEntityTypeConfiguration<SubscriptionUsage>
{
    public void Configure(EntityTypeBuilder<SubscriptionUsage> builder)
    {
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => new { u.UserId, u.Year, u.Month }).IsUnique();
        builder.Property(u => u.EmailsSent).HasDefaultValue(0);
    }
}
