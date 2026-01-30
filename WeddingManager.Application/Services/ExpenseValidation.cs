using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public static class ExpenseValidation
{
    public static Result ValidateInput(CreateWeddingExpenseRequestDto requestDto)
    {
        var errors = new List<Error>();

        if (requestDto.Amount <= 0)
            errors.Add(new Error(ErrorCodes.Validation, "Expense amount must be greater than 0"));

        if (string.IsNullOrWhiteSpace(requestDto.Description))
            errors.Add(new Error(ErrorCodes.Validation, "Expense description is required"));

        if (requestDto.Description.Length > 500)
            errors.Add(new Error(ErrorCodes.Validation, "Expense description must not exceed 500 characters"));

        if (requestDto.Notes != null && requestDto.Notes.Length > 1000)
            errors.Add(new Error(ErrorCodes.Validation, "Expense notes must not exceed 1000 characters"));

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    public static Result ValidateInput(UpdateWeddingExpenseRequestDto requestDto)
    {
        var errors = new List<Error>();

        if (requestDto.Amount <= 0)
            errors.Add(new Error(ErrorCodes.Validation, "Expense amount must be greater than 0"));

        if (string.IsNullOrWhiteSpace(requestDto.Description))
            errors.Add(new Error(ErrorCodes.Validation, "Expense description is required"));

        if (requestDto.Description.Length > 500)
            errors.Add(new Error(ErrorCodes.Validation, "Expense description must not exceed 500 characters"));

        if (requestDto.Notes != null && requestDto.Notes.Length > 1000)
            errors.Add(new Error(ErrorCodes.Validation, "Expense notes must not exceed 1000 characters"));

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }
}
