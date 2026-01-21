using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Content).IsRequired().HasColumnType("jsonb");

        builder.HasIndex(e => e.WeddingId);

        builder.HasOne(p => p.Wedding)
            .WithMany(w => w.Pages)
            .HasForeignKey(p => p.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
