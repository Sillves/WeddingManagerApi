using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.InvitationFlows;

public class GetInvitationFlowsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/invitation-flows",
                async (Guid weddingId, IInvitationFlowService flowService) =>
                {
                    var result = await flowService.GetByWeddingIdAsync(weddingId);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("InvitationFlows")
            .WithName("GetInvitationFlows")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces<IEnumerable<InvitationFlowDto>>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
