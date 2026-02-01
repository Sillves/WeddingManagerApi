using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class WeddingWebsiteEntityConfiguration : IEntityTypeConfiguration<WeddingWebsite>
{
    public void Configure(EntityTypeBuilder<WeddingWebsite> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Template)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Settings)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.Content)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(300);

        builder.HasIndex(e => e.WeddingId)
            .IsUnique();

        builder.HasOne(e => e.Wedding)
            .WithOne(w => w.Website)
            .HasForeignKey<WeddingWebsite>(e => e.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
