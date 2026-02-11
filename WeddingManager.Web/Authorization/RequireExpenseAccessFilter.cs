using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Web.Authorization;

public class RequireExpenseAccessFilter(
    IWeddingExpenseRepository expenseRepository,
    IUserContextService userContextService) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.RouteValues.TryGetValue("expenseId", out var expenseIdObj) ||
            !Guid.TryParse(expenseIdObj?.ToString(), out var expenseId))
        {
            return Results.BadRequest(new { error = "Invalid expense ID" });
        }

        var expense = await expenseRepository.GetByIdAsync(expenseId);
        if (expense == null)
        {
            return Results.NotFound(new { error = "Expense not found" });
        }

        var userId = userContextService.GetUserId();
        if (expense.Wedding.UserId != userId)
        {
            return Results.Forbid();
        }

        context.HttpContext.Items["Expense"] = expense;
        context.HttpContext.Items["Wedding"] = expense.Wedding;
        return await next(context);
    }
}
