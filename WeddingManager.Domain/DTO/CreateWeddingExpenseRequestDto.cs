using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class CreateWeddingExpenseRequestDto
{
    public decimal Amount { get; set; }
    public ExpenseCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
