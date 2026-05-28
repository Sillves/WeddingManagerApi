using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.InvitationFlows;

public class DeleteInvitationFlowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/weddings/{weddingId}/invitation-flows/{flowId:guid}",
                async (Guid weddingId, Guid flowId, bool? force, IInvitationFlowService flowService) =>
                {
                    var result = await flowService.DeleteAsync(weddingId, flowId, force ?? false);
                    return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
                })
            .WithTags("InvitationFlows")
            .WithName("DeleteInvitationFlow")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(409);
    }
}
