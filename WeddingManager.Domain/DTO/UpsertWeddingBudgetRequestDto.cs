namespace WeddingManager.Domain.DTO;

public class UpsertWeddingBudgetRequestDto
{
    public decimal TotalBudget { get; set; }
    public List<BudgetAllocationRequestDto> Allocations { get; set; } = new();
}
