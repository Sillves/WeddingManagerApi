using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingManager.Domain.Entities;

namespace WeddingManager.Infrastructure.Data.Configurations;

public class WeddingExpenseConfiguration : IEntityTypeConfiguration<WeddingExpense>
{
    public void Configure(EntityTypeBuilder<WeddingExpense> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount)
            .IsRequired()
            .HasPrecision(18, 2);
        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();
        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.WeddingId);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.Category);

        builder.HasOne(e => e.Wedding)
            .WithMany(w => w.Expenses)
            .HasForeignKey(e => e.WeddingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
