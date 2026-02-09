using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class BudgetValidationTests
{
    [Fact]
    public void ValidateInput_ReturnsSuccess_WhenInputIsValid()
    {
        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 25000m,
            Allocations = new List<BudgetAllocationRequestDto>
            {
                new() { Category = ExpenseCategory.Venue, Amount = 10000m },
                new() { Category = ExpenseCategory.Catering, Amount = 5000m }
            }
        };

        var result = BudgetValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateInput_ReturnsError_WhenTotalBudgetIsZero()
    {
        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 0m,
            Allocations = new List<BudgetAllocationRequestDto>()
        };

        var result = BudgetValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation && e.Message.Contains("Total budget"));
    }

    [Fact]
    public void ValidateInput_ReturnsError_WhenTotalBudgetIsNegative()
    {
        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = -1000m,
            Allocations = new List<BudgetAllocationRequestDto>()
        };

        var result = BudgetValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation && e.Message.Contains("Total budget"));
    }

    [Fact]
    public void ValidateInput_ReturnsError_WhenAllocationAmountIsNegative()
    {
        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 25000m,
            Allocations = new List<BudgetAllocationRequestDto>
            {
                new() { Category = ExpenseCategory.Venue, Amount = -500m }
            }
        };

        var result = BudgetValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation && e.Message.Contains("negative"));
    }

    [Fact]
    public void ValidateInput_ReturnsError_WhenDuplicateCategoriesExist()
    {
        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 25000m,
            Allocations = new List<BudgetAllocationRequestDto>
            {
                new() { Category = ExpenseCategory.Venue, Amount = 10000m },
                new() { Category = ExpenseCategory.Venue, Amount = 5000m }
            }
        };

        var result = BudgetValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation && e.Message.Contains("Duplicate"));
    }
}
