using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class ExpenseValidationTests
{
    [Fact]
    public void ValidateInput_Create_ReturnsOkForValidInput()
    {
        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = 1500.00m,
            Category = ExpenseCategory.Venue,
            Description = "Wedding venue deposit",
            Date = DateTime.UtcNow,
            Notes = "50% deposit"
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateInput_Create_ReturnsErrorForZeroAmount()
    {
        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = 0m,
            Category = ExpenseCategory.Venue,
            Description = "Free venue",
            Date = DateTime.UtcNow
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense amount must be greater than 0");
    }

    [Fact]
    public void ValidateInput_Create_ReturnsErrorForNegativeAmount()
    {
        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = -100.00m,
            Category = ExpenseCategory.Venue,
            Description = "Refund",
            Date = DateTime.UtcNow
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense amount must be greater than 0");
    }

    [Fact]
    public void ValidateInput_Create_ReturnsErrorForMissingDescription()
    {
        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = 100.00m,
            Category = ExpenseCategory.Venue,
            Description = "",
            Date = DateTime.UtcNow
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense description is required");
    }

    [Fact]
    public void ValidateInput_Create_ReturnsErrorForDescriptionTooLong()
    {
        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = 100.00m,
            Category = ExpenseCategory.Venue,
            Description = new string('A', 501),
            Date = DateTime.UtcNow
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense description must not exceed 500 characters");
    }

    [Fact]
    public void ValidateInput_Create_ReturnsErrorForNotesTooLong()
    {
        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = 100.00m,
            Category = ExpenseCategory.Venue,
            Description = "Valid description",
            Date = DateTime.UtcNow,
            Notes = new string('N', 1001)
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense notes must not exceed 1000 characters");
    }

    [Fact]
    public void ValidateInput_Create_ReturnsOkForNotesWithinLimit()
    {
        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = 100.00m,
            Category = ExpenseCategory.Venue,
            Description = "Valid description",
            Date = DateTime.UtcNow,
            Notes = new string('N', 1000)
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateInput_Update_ReturnsOkForValidInput()
    {
        var request = new UpdateWeddingExpenseRequestDto
        {
            Amount = 2000.00m,
            Category = ExpenseCategory.Catering,
            Description = "Catering update",
            Date = DateTime.UtcNow,
            Notes = "Revised quote"
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorForInvalidAmount()
    {
        var request = new UpdateWeddingExpenseRequestDto
        {
            Amount = -50.00m,
            Category = ExpenseCategory.Catering,
            Description = "Update",
            Date = DateTime.UtcNow
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense amount must be greater than 0");
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorForMissingDescription()
    {
        var request = new UpdateWeddingExpenseRequestDto
        {
            Amount = 100.00m,
            Category = ExpenseCategory.Catering,
            Description = "   ",
            Date = DateTime.UtcNow
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense description is required");
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorForDescriptionTooLong()
    {
        var request = new UpdateWeddingExpenseRequestDto
        {
            Amount = 100.00m,
            Category = ExpenseCategory.Catering,
            Description = new string('B', 501),
            Date = DateTime.UtcNow
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense description must not exceed 500 characters");
    }

    [Fact]
    public void ValidateInput_Update_ReturnsErrorForNotesTooLong()
    {
        var request = new UpdateWeddingExpenseRequestDto
        {
            Amount = 100.00m,
            Category = ExpenseCategory.Catering,
            Description = "Valid",
            Date = DateTime.UtcNow,
            Notes = new string('X', 1001)
        };

        var result = ExpenseValidation.ValidateInput(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Expense notes must not exceed 1000 characters");
    }
}
