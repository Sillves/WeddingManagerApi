using Microsoft.Extensions.Logging;
using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingBudgetService(
    IWeddingBudgetRepository budgetRepository,
    IWeddingRepository weddingRepository,
    ApplicationMapper mapper,
    ILogger<WeddingBudgetService> logger)
    : IWeddingBudgetService
{
    public async Task<Result<WeddingBudgetDto?>> GetByWeddingIdAsync(Guid weddingId)
    {
        var budget = await budgetRepository.GetByWeddingIdAsync(weddingId);
        if (budget == null)
        {
            return Result<WeddingBudgetDto?>.Ok(null);
        }

        return Result<WeddingBudgetDto?>.Ok(mapper.BudgetToDto(budget));
    }

    public async Task<Result<WeddingBudgetDto>> UpsertBudgetAsync(Guid weddingId, UpsertWeddingBudgetRequestDto request)
    {
        var validationResult = BudgetValidation.ValidateInput(request);
        if (!validationResult.IsSuccess)
        {
            return Result<WeddingBudgetDto>.Fail(validationResult.Errors);
        }

        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding == null)
        {
            return Result<WeddingBudgetDto>.Fail(new Error(ErrorCodes.NotFound, $"Wedding with id {weddingId} not found"));
        }

        var now = DateTime.UtcNow;
        var budget = new WeddingBudget
        {
            Id = Guid.NewGuid(),
            WeddingId = weddingId,
            TotalBudget = request.TotalBudget,
            CreatedAt = now,
            UpdatedAt = now,
            Allocations = request.Allocations.Select(a => new BudgetAllocation
            {
                Id = Guid.NewGuid(),
                Category = a.Category,
                Amount = a.Amount
            }).ToList()
        };

        await budgetRepository.UpsertAsync(budget);
        logger.LogInformation("Upserted budget for wedding {WeddingId}", weddingId);

        var saved = await budgetRepository.GetByWeddingIdAsync(weddingId);
        return Result<WeddingBudgetDto>.Ok(mapper.BudgetToDto(saved!));
    }
}
