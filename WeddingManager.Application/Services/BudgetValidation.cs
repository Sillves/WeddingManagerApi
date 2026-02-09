using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public static class BudgetValidation
{
    public static Result ValidateInput(UpsertWeddingBudgetRequestDto requestDto)
    {
        var errors = new List<Error>();

        if (requestDto.TotalBudget <= 0)
            errors.Add(new Error(ErrorCodes.Validation, "Total budget must be greater than 0"));

        var seenCategories = new HashSet<int>();
        foreach (var allocation in requestDto.Allocations)
        {
            if (allocation.Amount < 0)
                errors.Add(new Error(ErrorCodes.Validation, $"Allocation amount for {allocation.Category} must not be negative"));

            if (!seenCategories.Add((int)allocation.Category))
                errors.Add(new Error(ErrorCodes.Validation, $"Duplicate allocation for category {allocation.Category}"));
        }

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }
}
