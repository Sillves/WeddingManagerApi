using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Expenses;

public class GetExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expenses/{expenseId}",
            async (Guid expenseId, IWeddingExpenseService expenseService) =>
            {
                var result = await expenseService.GetByIdAsync(expenseId);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .AddEndpointFilter<RequireExpenseAccessFilter>()
            .WithTags("Expenses")
            .WithName("GetExpense")
            .RequireAuthorization()
            .Produces<WeddingExpenseDto>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(404);
    }
}
