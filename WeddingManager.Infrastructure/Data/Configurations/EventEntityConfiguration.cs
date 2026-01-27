using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class EventEntityConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.StartDate).IsRequired();
        builder.Property(e => e.Location).IsRequired().HasMaxLength(200);
        builder.HasOne(e => e.Wedding)
            .WithMany(w => w.Events)
            .HasForeignKey(e => e.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.WeddingId, e.Name }).IsUnique();

        builder.HasMany(e => e.Guests)
            .WithMany(g => g.Events)
            .UsingEntity<Dictionary<string, object>>(
                "EventGuests",
                right => right
                    .HasOne<Guest>()
                    .WithMany()
                    .HasForeignKey("GuestId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Event>()
                    .WithMany()
                    .HasForeignKey("EventId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("EventId", "GuestId");
                    join.HasIndex("GuestId");
                    join.ToTable("EventGuests");
                });
    }
}
