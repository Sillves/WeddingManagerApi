namespace WeddingManager.Domain.Entities;

public class WeddingBudget
{
    public Guid Id { get; set; }
    public Guid WeddingId { get; set; }
    public decimal TotalBudget { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Wedding Wedding { get; set; } = null!;
    public ICollection<BudgetAllocation> Allocations { get; set; } = new List<BudgetAllocation>();
}
