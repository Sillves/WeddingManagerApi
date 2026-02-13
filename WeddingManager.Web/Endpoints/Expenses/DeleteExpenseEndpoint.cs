using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Expenses;

public class DeleteExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/expenses/{expenseId}",
            async (Guid expenseId, IWeddingExpenseService expenseService) =>
            {
                var result = await expenseService.DeleteExpenseAsync(expenseId);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
            .AddEndpointFilter<RequireExpenseAccessFilter>()
            .RequireModuleAccess(WeddingModule.Expenses)
            .WithTags("Expenses")
            .WithName("DeleteExpense")
            .RequireAuthorization()
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(404);
    }
}
