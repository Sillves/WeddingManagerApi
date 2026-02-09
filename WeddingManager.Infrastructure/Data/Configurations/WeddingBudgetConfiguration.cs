using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class WeddingBudgetConfiguration : IEntityTypeConfiguration<WeddingBudget>
{
    public void Configure(EntityTypeBuilder<WeddingBudget> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.TotalBudget)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        builder.HasIndex(b => b.WeddingId).IsUnique();

        builder.HasOne(b => b.Wedding)
            .WithOne(w => w.Budget)
            .HasForeignKey<WeddingBudget>(b => b.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
