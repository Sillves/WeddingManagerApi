using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class BudgetAllocationConfiguration : IEntityTypeConfiguration<BudgetAllocation>
{
    public void Configure(EntityTypeBuilder<BudgetAllocation> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(a => a.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(a => new { a.WeddingBudgetId, a.Category }).IsUnique();

        builder.HasOne(a => a.WeddingBudget)
            .WithMany(b => b.Allocations)
            .HasForeignKey(a => a.WeddingBudgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
