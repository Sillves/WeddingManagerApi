using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Entities;

public class BudgetAllocation
{
    public Guid Id { get; set; }
    public Guid WeddingBudgetId { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public WeddingBudget WeddingBudget { get; set; } = null!;
}
