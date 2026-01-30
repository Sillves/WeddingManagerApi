using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Expenses;

public class GetExpensesByWedding : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/weddings/{weddingId}/expenses",
            async (Guid weddingId, IWeddingExpenseService expenseService) =>
            {
                var result = await expenseService.GetByWeddingIdAsync(weddingId);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("Expenses")
            .WithName("GetExpensesByWedding")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<IEnumerable<WeddingExpenseDto>>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
