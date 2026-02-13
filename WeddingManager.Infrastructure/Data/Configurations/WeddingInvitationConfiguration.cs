using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class WeddingInvitationConfiguration : IEntityTypeConfiguration<WeddingInvitation>
{
    public void Configure(EntityTypeBuilder<WeddingInvitation> builder)
    {
        builder.HasKey(wi => wi.Id);

        builder.Property(wi => wi.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(wi => wi.Token)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(wi => wi.Token).IsUnique();

        builder.Property(wi => wi.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.HasOne(wi => wi.Wedding)
            .WithMany()
            .HasForeignKey(wi => wi.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
