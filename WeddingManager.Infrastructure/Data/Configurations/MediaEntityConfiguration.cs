using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.S3Url).IsRequired();
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.WeddingId);

        builder.HasOne(m => m.Wedding)
            .WithMany(w => w.Media)
            .HasForeignKey(m => m.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
