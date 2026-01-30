using AutoMapper;
using Microsoft.Extensions.Logging;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingExpenseService(
    IWeddingExpenseRepository expenseRepository,
    IWeddingRepository weddingRepository,
    IMapper mapper,
    ILogger<WeddingExpenseService> logger)
    : IWeddingExpenseService
{
    public async Task<Result<WeddingExpenseDto>> GetByIdAsync(Guid expenseId)
    {
        var expense = await expenseRepository.GetByIdAsync(expenseId);
        return expense == null
            ? Result<WeddingExpenseDto>.Fail(new Error(ErrorCodes.NotFound, $"Expense with id {expenseId} not found"))
            : Result<WeddingExpenseDto>.Ok(mapper.Map<WeddingExpenseDto>(expense));
    }

    public async Task<Result<IEnumerable<WeddingExpenseDto>>> GetByWeddingIdAsync(Guid weddingId)
    {
        var expenses = await expenseRepository.GetByWeddingIdAsync(weddingId);
        return Result<IEnumerable<WeddingExpenseDto>>.Ok(mapper.Map<IEnumerable<WeddingExpenseDto>>(expenses));
    }

    public async Task<Result<IEnumerable<WeddingExpenseDto>>> GetByWeddingIdAndCategoryAsync(Guid weddingId, ExpenseCategory category)
    {
        var expenses = await expenseRepository.GetByWeddingIdAndCategoryAsync(weddingId, category);
        return Result<IEnumerable<WeddingExpenseDto>>.Ok(mapper.Map<IEnumerable<WeddingExpenseDto>>(expenses));
    }

    public async Task<Result<WeddingExpenseSummaryDto>> GetSummaryAsync(Guid weddingId)
    {
        var expenses = await expenseRepository.GetByWeddingIdAsync(weddingId);
        var categoryTotals = await expenseRepository.GetCategoryTotalsAsync(weddingId);
        var totalAmount = await expenseRepository.GetTotalAmountAsync(weddingId);

        var summary = new WeddingExpenseSummaryDto
        {
            TotalAmount = totalAmount,
            CategoryTotals = categoryTotals,
            Expenses = mapper.Map<List<WeddingExpenseDto>>(expenses)
        };

        return Result<WeddingExpenseSummaryDto>.Ok(summary);
    }

    public async Task<Result<WeddingExpenseDto>> CreateExpenseAsync(Guid weddingId, CreateWeddingExpenseRequestDto requestDto)
    {
        var validationResult = ExpenseValidation.ValidateInput(requestDto);
        if (!validationResult.IsSuccess)
        {
            return Result<WeddingExpenseDto>.Fail(validationResult.Errors);
        }

        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding == null)
        {
            return Result<WeddingExpenseDto>.Fail(new Error(ErrorCodes.NotFound, $"Wedding with id {weddingId} not found"));
        }

        var expense = new WeddingExpense
        {
            Id = Guid.NewGuid(),
            WeddingId = weddingId,
            Amount = requestDto.Amount,
            Category = requestDto.Category,
            Description = requestDto.Description,
            Date = requestDto.Date,
            Notes = requestDto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await expenseRepository.AddAsync(expense);
        logger.LogInformation("Created expense {ExpenseId} for wedding {WeddingId}", expense.Id, weddingId);

        return Result<WeddingExpenseDto>.Ok(mapper.Map<WeddingExpenseDto>(expense));
    }

    public async Task<Result<WeddingExpenseDto>> UpdateExpenseAsync(Guid expenseId, UpdateWeddingExpenseRequestDto requestDto)
    {
        var validationResult = ExpenseValidation.ValidateInput(requestDto);
        if (!validationResult.IsSuccess)
        {
            return Result<WeddingExpenseDto>.Fail(validationResult.Errors);
        }

        var expense = await expenseRepository.GetByIdAsync(expenseId);
        if (expense == null)
        {
            return Result<WeddingExpenseDto>.Fail(new Error(ErrorCodes.NotFound, $"Expense with id {expenseId} not found"));
        }

        expense.Amount = requestDto.Amount;
        expense.Category = requestDto.Category;
        expense.Description = requestDto.Description;
        expense.Date = requestDto.Date;
        expense.Notes = requestDto.Notes;
        expense.UpdatedAt = DateTime.UtcNow;

        await expenseRepository.UpdateAsync(expense);
        logger.LogInformation("Updated expense {ExpenseId}", expenseId);

        return Result<WeddingExpenseDto>.Ok(mapper.Map<WeddingExpenseDto>(expense));
    }

    public async Task<Result> DeleteExpenseAsync(Guid expenseId)
    {
        var expense = await expenseRepository.GetByIdAsync(expenseId);
        if (expense == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, $"Expense with id {expenseId} not found"));
        }

        await expenseRepository.DeleteAsync(expenseId);
        logger.LogInformation("Deleted expense {ExpenseId}", expenseId);

        return Result.Ok();
    }
}
