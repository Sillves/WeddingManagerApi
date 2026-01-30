using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class WeddingExpenseSummaryDto
{
    public decimal TotalAmount { get; set; }
    public Dictionary<ExpenseCategory, decimal> CategoryTotals { get; set; } = new();
    public List<WeddingExpenseDto> Expenses { get; set; } = new();
}
