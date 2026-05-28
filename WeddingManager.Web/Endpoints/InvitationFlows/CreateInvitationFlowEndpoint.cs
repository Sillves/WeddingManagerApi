using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.InvitationFlows;

public class CreateInvitationFlowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/invitation-flows",
                async (Guid weddingId, CreateInvitationFlowRequestDto requestDto, IInvitationFlowService flowService) =>
                {
                    var result = await flowService.CreateAsync(weddingId, requestDto);
                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    var flow = result.Value!;
                    return Results.Created($"/api/weddings/{weddingId}/invitation-flows/{flow.Id}", flow);
                })
            .WithTags("InvitationFlows")
            .WithName("CreateInvitationFlow")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces<InvitationFlowDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(409);
    }
}
