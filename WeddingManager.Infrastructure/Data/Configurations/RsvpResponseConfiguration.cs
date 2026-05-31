using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class RsvpResponseConfiguration : IEntityTypeConfiguration<RsvpResponse>
{
    public void Configure(EntityTypeBuilder<RsvpResponse> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.AttendingEventIds)
            .HasColumnType("jsonb")
            .HasConversion(JsonbConverters.ListConverter<Guid>())
            .Metadata.SetValueComparer(JsonbConverters.ListComparer<Guid>());

        builder.Property(e => e.CustomAnswers)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.HasIndex(e => new { e.WeddingId, e.InvitationFlowId });
        builder.HasIndex(e => e.GuestId);

        builder.HasOne(e => e.Wedding)
            .WithMany()
            .HasForeignKey(e => e.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.InvitationFlow)
            .WithMany(f => f.Responses)
            .HasForeignKey(e => e.InvitationFlowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Guest)
            .WithMany()
            .HasForeignKey(e => e.GuestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
