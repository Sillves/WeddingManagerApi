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
        builder.Property(e => e.Surname).HasMaxLength(255);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Dietary);
        builder.Property(e => e.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en");
        builder.Property(e => e.InvitationToken).HasMaxLength(200);

        builder.Property(e => e.RsvpStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        // Composite dedupe key is case-insensitive on email/name/surname; created as a
        // functional unique index via raw SQL in the migration (lower(...) can't be
        // expressed through the fluent API). The old (WeddingId, Email) index is dropped there.
        builder.HasIndex(e => e.WeddingId);
        builder.HasIndex(e => e.InvitationToken).IsUnique();

        builder.HasOne(g => g.Wedding)
            .WithMany(w => w.Guests)
            .HasForeignKey(g => g.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.PlusOneOf)
            .WithMany()
            .HasForeignKey(g => g.PlusOneOfGuestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
