using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.InvitationFlows;

public class UpdateInvitationFlowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/weddings/{weddingId}/invitation-flows/{flowId:guid}",
                async (Guid weddingId, Guid flowId, UpdateInvitationFlowRequestDto requestDto,
                    IInvitationFlowService flowService) =>
                {
                    var result = await flowService.UpdateAsync(weddingId, flowId, requestDto);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("InvitationFlows")
            .WithName("UpdateInvitationFlow")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces<InvitationFlowDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(409);
    }
}
