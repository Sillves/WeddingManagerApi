using Moq;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class WeddingExpenseServiceTests
{
    private readonly ApplicationMapper _mapper = new();

    [Fact]
    public async Task CreateExpenseAsync_ValidatesInputAndAddsExpense()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding { Id = weddingId };

        var expenseRepositoryMock = new Mock<IWeddingExpenseRepository>();
        expenseRepositoryMock.Setup(r => r.AddAsync(It.IsAny<WeddingExpense>()))
            .Returns(Task.CompletedTask);

        var weddingRepositoryMock = new Mock<IWeddingRepository>();
        weddingRepositoryMock.Setup(r => r.GetByIdAsync(weddingId))
            .ReturnsAsync(wedding);

        var service = new WeddingExpenseService(
            expenseRepositoryMock.Object,
            weddingRepositoryMock.Object,
            _mapper,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WeddingExpenseService>>());

        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = 1500.00m,
            Category = ExpenseCategory.Venue,
            Description = "Wedding venue deposit",
            Date = DateTime.UtcNow,
            Notes = "50% deposit"
        };

        var result = await service.CreateExpenseAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        expenseRepositoryMock.Verify(r => r.AddAsync(It.Is<WeddingExpense>(e =>
            e.WeddingId == weddingId &&
            e.Amount == request.Amount &&
            e.Category == request.Category &&
            e.Description == request.Description)), Times.Once);
        Assert.Equal(request.Amount, result.Value!.Amount);
        Assert.Equal(request.Category, result.Value.Category);
    }

    [Fact]
    public async Task CreateExpenseAsync_RejectsInvalidAmount()
    {
        var weddingId = Guid.NewGuid();
        var expenseRepositoryMock = new Mock<IWeddingExpenseRepository>();
        var weddingRepositoryMock = new Mock<IWeddingRepository>();

        var service = new WeddingExpenseService(
            expenseRepositoryMock.Object,
            weddingRepositoryMock.Object,
            _mapper,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WeddingExpenseService>>());

        var request = new CreateWeddingExpenseRequestDto
        {
            Amount = -100.00m,
            Category = ExpenseCategory.Venue,
            Description = "Invalid expense",
            Date = DateTime.UtcNow
        };

        var result = await service.CreateExpenseAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation);
        expenseRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WeddingExpense>()), Times.Never);
    }

    [Fact]
    public async Task UpdateExpenseAsync_ValidatesInputAndUpdatesExpense()
    {
        var expenseId = Guid.NewGuid();
        var weddingId = Guid.NewGuid();
        var existingExpense = new WeddingExpense
        {
            Id = expenseId,
            WeddingId = weddingId,
            Amount = 1000.00m,
            Category = ExpenseCategory.Venue,
            Description = "Original description",
            Date = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expenseRepositoryMock = new Mock<IWeddingExpenseRepository>();
        expenseRepositoryMock.Setup(r => r.GetByIdAsync(expenseId))
            .ReturnsAsync(existingExpense);
        expenseRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<WeddingExpense>()))
            .Returns(Task.CompletedTask);

        var weddingRepositoryMock = new Mock<IWeddingRepository>();

        var service = new WeddingExpenseService(
            expenseRepositoryMock.Object,
            weddingRepositoryMock.Object,
            _mapper,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WeddingExpenseService>>());

        var request = new UpdateWeddingExpenseRequestDto
        {
            Amount = 1500.00m,
            Category = ExpenseCategory.Venue,
            Description = "Updated description",
            Date = DateTime.UtcNow,
            Notes = "Full payment"
        };

        var result = await service.UpdateExpenseAsync(expenseId, request);

        Assert.True(result.IsSuccess);
        expenseRepositoryMock.Verify(r => r.UpdateAsync(It.Is<WeddingExpense>(e =>
            e.Id == expenseId &&
            e.Amount == request.Amount &&
            e.Description == request.Description)), Times.Once);
        Assert.Equal(request.Amount, result.Value!.Amount);
        Assert.Equal(request.Description, result.Value.Description);
    }

    [Fact]
    public async Task DeleteExpenseAsync_DeletesExistingExpense()
    {
        var expenseId = Guid.NewGuid();
        var existingExpense = new WeddingExpense
        {
            Id = expenseId,
            WeddingId = Guid.NewGuid(),
            Amount = 1000.00m,
            Category = ExpenseCategory.Venue,
            Description = "Test expense",
            Date = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expenseRepositoryMock = new Mock<IWeddingExpenseRepository>();
        expenseRepositoryMock.Setup(r => r.GetByIdAsync(expenseId))
            .ReturnsAsync(existingExpense);
        expenseRepositoryMock.Setup(r => r.DeleteAsync(expenseId))
            .Returns(Task.CompletedTask);

        var weddingRepositoryMock = new Mock<IWeddingRepository>();

        var service = new WeddingExpenseService(
            expenseRepositoryMock.Object,
            weddingRepositoryMock.Object,
            _mapper,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WeddingExpenseService>>());

        var result = await service.DeleteExpenseAsync(expenseId);

        Assert.True(result.IsSuccess);
        expenseRepositoryMock.Verify(r => r.DeleteAsync(expenseId), Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsTotalsAndBreakdown()
    {
        var weddingId = Guid.NewGuid();
        var expenses = new List<WeddingExpense>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                Amount = 5000.00m,
                Category = ExpenseCategory.Venue,
                Description = "Venue",
                Date = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                Amount = 3000.00m,
                Category = ExpenseCategory.Catering,
                Description = "Catering",
                Date = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var categoryTotals = new Dictionary<ExpenseCategory, decimal>
        {
            { ExpenseCategory.Venue, 5000.00m },
            { ExpenseCategory.Catering, 3000.00m }
        };

        var expenseRepositoryMock = new Mock<IWeddingExpenseRepository>();
        expenseRepositoryMock.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(expenses);
        expenseRepositoryMock.Setup(r => r.GetCategoryTotalsAsync(weddingId))
            .ReturnsAsync(categoryTotals);
        expenseRepositoryMock.Setup(r => r.GetTotalAmountAsync(weddingId))
            .ReturnsAsync(8000.00m);

        var weddingRepositoryMock = new Mock<IWeddingRepository>();

        var service = new WeddingExpenseService(
            expenseRepositoryMock.Object,
            weddingRepositoryMock.Object,
            _mapper,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WeddingExpenseService>>());

        var result = await service.GetSummaryAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.Equal(8000.00m, result.Value!.TotalAmount);
        Assert.Equal(2, result.Value.CategoryTotals.Count);
        Assert.Equal(5000.00m, result.Value.CategoryTotals[ExpenseCategory.Venue]);
        Assert.Equal(3000.00m, result.Value.CategoryTotals[ExpenseCategory.Catering]);
        Assert.Equal(2, result.Value.Expenses.Count);
    }
}
