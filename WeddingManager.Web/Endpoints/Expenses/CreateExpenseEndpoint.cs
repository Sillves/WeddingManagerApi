using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Expenses;

public class CreateExpenseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/weddings/{weddingId}/expenses",
            async (Guid weddingId, CreateWeddingExpenseRequestDto request, IWeddingExpenseService expenseService) =>
            {
                var result = await expenseService.CreateExpenseAsync(weddingId, request);
                return result.IsSuccess
                    ? Results.Created($"/api/expenses/{result.Value!.Id}", result.Value)
                    : result.ToErrorResult();
            })
            .WithTags("Expenses")
            .WithName("CreateExpense")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Expenses)
            .Produces<WeddingExpenseDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
