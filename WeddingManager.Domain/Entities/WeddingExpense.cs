using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class WeddingExpense
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public decimal Amount { get; set; }
    public ExpenseCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Wedding Wedding { get; set; } = null!;
}
