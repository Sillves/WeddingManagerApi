using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class BudgetAllocationRequestDto
{
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
}
