namespace WeddingManager.Domain.DTO;

public class WeddingBudgetDto
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public decimal TotalBudget { get; set; }
    public List<BudgetAllocationDto> Allocations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
