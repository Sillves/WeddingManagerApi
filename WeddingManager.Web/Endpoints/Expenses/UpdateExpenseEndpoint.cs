using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Expenses;

public class UpdateExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/expenses/{expenseId}",
            async (Guid expenseId, UpdateWeddingExpenseRequestDto request, IWeddingExpenseService expenseService) =>
            {
                var result = await expenseService.UpdateExpenseAsync(expenseId, request);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .AddEndpointFilter<RequireExpenseAccessFilter>()
            .RequireModuleAccess(WeddingModule.Expenses)
            .WithTags("Expenses")
            .WithName("UpdateExpense")
            .RequireAuthorization()
            .Produces<WeddingExpenseDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(404);
    }
}
