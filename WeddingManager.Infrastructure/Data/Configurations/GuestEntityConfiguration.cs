using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(255);

        builder.HasIndex(e => new { e.WeddingId, e.Email }).IsUnique();
        builder.HasIndex(e => e.WeddingId);

        builder.HasOne(g => g.Wedding)
            .WithMany(w => w.Guests)
            .HasForeignKey(g => g.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
