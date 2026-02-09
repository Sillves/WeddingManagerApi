using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class BudgetAllocationDto
{
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
}
