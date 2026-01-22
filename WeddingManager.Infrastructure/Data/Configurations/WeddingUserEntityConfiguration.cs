
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class WeddingUserConfiguration : IEntityTypeConfiguration<WeddingUser>
{
    public void Configure(EntityTypeBuilder<WeddingUser> builder)
    {
        builder.HasKey(wu => new { wu.WeddingId, wu.UserId });

        builder.Property(wu => wu.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.HasOne(wu => wu.Wedding)
            .WithMany(w => w.WeddingUsers)
            .HasForeignKey(wu => wu.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wu => wu.User)
            .WithMany(u => u.WeddingUsers)
            .HasForeignKey(wu => wu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(wu => wu.UserId);
    }
}
