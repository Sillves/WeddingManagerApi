using Moq;
using WeddingManager.Application.Mappings;
using WeddingManager.Application.Services;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Tests;

public class WeddingBudgetServiceTests
{
    private readonly ApplicationMapper _mapper = new();

    private WeddingBudgetService CreateService(
        Mock<IWeddingBudgetRepository>? budgetRepo = null,
        Mock<IWeddingRepository>? weddingRepo = null)
    {
        return new WeddingBudgetService(
            budgetRepo?.Object ?? new Mock<IWeddingBudgetRepository>().Object,
            weddingRepo?.Object ?? new Mock<IWeddingRepository>().Object,
            _mapper,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WeddingBudgetService>>());
    }

    [Fact]
    public async Task UpsertBudgetAsync_CreatesNewBudget_WhenNoneExists()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding { Id = weddingId };

        var budgetRepoMock = new Mock<IWeddingBudgetRepository>();
        budgetRepoMock.Setup(r => r.UpsertAsync(It.IsAny<WeddingBudget>()))
            .Returns(Task.CompletedTask);
        budgetRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(new WeddingBudget
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                TotalBudget = 25000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Allocations = new List<BudgetAllocation>
                {
                    new() { Id = Guid.NewGuid(), Category = ExpenseCategory.Venue, Amount = 10000m }
                }
            });

        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId))
            .ReturnsAsync(wedding);

        var service = CreateService(budgetRepoMock, weddingRepoMock);

        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 25000m,
            Allocations = new List<BudgetAllocationRequestDto>
            {
                new() { Category = ExpenseCategory.Venue, Amount = 10000m }
            }
        };

        var result = await service.UpsertBudgetAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(25000m, result.Value!.TotalBudget);
        Assert.Single(result.Value.Allocations);
        Assert.Equal(ExpenseCategory.Venue, result.Value.Allocations[0].Category);
        Assert.Equal(10000m, result.Value.Allocations[0].Amount);
        budgetRepoMock.Verify(r => r.UpsertAsync(It.Is<WeddingBudget>(b =>
            b.WeddingId == weddingId && b.TotalBudget == 25000m)), Times.Once);
    }

    [Fact]
    public async Task UpsertBudgetAsync_UpdatesExistingBudget_WhenAlreadyExists()
    {
        var weddingId = Guid.NewGuid();
        var wedding = new Wedding { Id = weddingId };

        var budgetRepoMock = new Mock<IWeddingBudgetRepository>();
        budgetRepoMock.Setup(r => r.UpsertAsync(It.IsAny<WeddingBudget>()))
            .Returns(Task.CompletedTask);
        budgetRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(new WeddingBudget
            {
                Id = Guid.NewGuid(),
                WeddingId = weddingId,
                TotalBudget = 30000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Allocations = new List<BudgetAllocation>
                {
                    new() { Id = Guid.NewGuid(), Category = ExpenseCategory.Venue, Amount = 15000m },
                    new() { Id = Guid.NewGuid(), Category = ExpenseCategory.Catering, Amount = 8000m }
                }
            });

        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId))
            .ReturnsAsync(wedding);

        var service = CreateService(budgetRepoMock, weddingRepoMock);

        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 30000m,
            Allocations = new List<BudgetAllocationRequestDto>
            {
                new() { Category = ExpenseCategory.Venue, Amount = 15000m },
                new() { Category = ExpenseCategory.Catering, Amount = 8000m }
            }
        };

        var result = await service.UpsertBudgetAsync(weddingId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(30000m, result.Value!.TotalBudget);
        Assert.Equal(2, result.Value.Allocations.Count);
        budgetRepoMock.Verify(r => r.UpsertAsync(It.IsAny<WeddingBudget>()), Times.Once);
    }

    [Fact]
    public async Task UpsertBudgetAsync_RejectsInvalidTotalBudget()
    {
        var weddingId = Guid.NewGuid();

        var service = CreateService();

        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = -100m,
            Allocations = new List<BudgetAllocationRequestDto>()
        };

        var result = await service.UpsertBudgetAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation);
    }

    [Fact]
    public async Task UpsertBudgetAsync_RejectsNegativeAllocationAmount()
    {
        var weddingId = Guid.NewGuid();

        var service = CreateService();

        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 25000m,
            Allocations = new List<BudgetAllocationRequestDto>
            {
                new() { Category = ExpenseCategory.Venue, Amount = -500m }
            }
        };

        var result = await service.UpsertBudgetAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation && e.Message.Contains("negative"));
    }

    [Fact]
    public async Task UpsertBudgetAsync_RejectsDuplicateCategories()
    {
        var weddingId = Guid.NewGuid();

        var service = CreateService();

        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 25000m,
            Allocations = new List<BudgetAllocationRequestDto>
            {
                new() { Category = ExpenseCategory.Venue, Amount = 10000m },
                new() { Category = ExpenseCategory.Venue, Amount = 5000m }
            }
        };

        var result = await service.UpsertBudgetAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Validation && e.Message.Contains("Duplicate"));
    }

    [Fact]
    public async Task UpsertBudgetAsync_ReturnsNotFound_WhenWeddingDoesNotExist()
    {
        var weddingId = Guid.NewGuid();

        var weddingRepoMock = new Mock<IWeddingRepository>();
        weddingRepoMock.Setup(r => r.GetByIdAsync(weddingId))
            .ReturnsAsync((Wedding?)null);

        var service = CreateService(weddingRepo: weddingRepoMock);

        var request = new UpsertWeddingBudgetRequestDto
        {
            TotalBudget = 25000m,
            Allocations = new List<BudgetAllocationRequestDto>()
        };

        var result = await service.UpsertBudgetAsync(weddingId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == ErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByWeddingIdAsync_ReturnsNull_WhenNoBudgetExists()
    {
        var weddingId = Guid.NewGuid();

        var budgetRepoMock = new Mock<IWeddingBudgetRepository>();
        budgetRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync((WeddingBudget?)null);

        var service = CreateService(budgetRepoMock);

        var result = await service.GetByWeddingIdAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetByWeddingIdAsync_ReturnsBudgetWithAllocations()
    {
        var weddingId = Guid.NewGuid();
        var budget = new WeddingBudget
        {
            Id = Guid.NewGuid(),
            WeddingId = weddingId,
            TotalBudget = 25000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Allocations = new List<BudgetAllocation>
            {
                new() { Id = Guid.NewGuid(), Category = ExpenseCategory.Venue, Amount = 10000m },
                new() { Id = Guid.NewGuid(), Category = ExpenseCategory.Catering, Amount = 5000m }
            }
        };

        var budgetRepoMock = new Mock<IWeddingBudgetRepository>();
        budgetRepoMock.Setup(r => r.GetByWeddingIdAsync(weddingId))
            .ReturnsAsync(budget);

        var service = CreateService(budgetRepoMock);

        var result = await service.GetByWeddingIdAsync(weddingId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(25000m, result.Value!.TotalBudget);
        Assert.Equal(2, result.Value.Allocations.Count);
    }
}
