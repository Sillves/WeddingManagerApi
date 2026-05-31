using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Models;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class InvitationFlowConfiguration : IEntityTypeConfiguration<InvitationFlow>
{
    public void Configure(EntityTypeBuilder<InvitationFlow> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Passcode).HasMaxLength(50);
        builder.Property(e => e.IncludePlusOne).IsRequired();

        builder.Property(e => e.CustomQuestions)
            .HasColumnType("jsonb")
            .HasConversion(JsonbConverters.ListConverter<QuestionDefinition>())
            .Metadata.SetValueComparer(JsonbConverters.ListComparer<QuestionDefinition>());

        builder.Property(e => e.EventIds)
            .HasColumnType("jsonb")
            .HasConversion(JsonbConverters.ListConverter<Guid>())
            .Metadata.SetValueComparer(JsonbConverters.ListComparer<Guid>());

        builder.Property(e => e.CustomEvents)
            .HasColumnType("jsonb")
            .HasConversion(JsonbConverters.ListConverter<CustomEventDefinition>())
            .Metadata.SetValueComparer(JsonbConverters.ListComparer<CustomEventDefinition>());

        builder.HasIndex(e => e.WeddingId);

        // At most one flow per (wedding, passcode) when a passcode is set.
        builder.HasIndex(e => new { e.WeddingId, e.Passcode })
            .IsUnique()
            .HasFilter("\"Passcode\" IS NOT NULL");

        builder.HasOne(e => e.Wedding)
            .WithMany()
            .HasForeignKey(e => e.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
