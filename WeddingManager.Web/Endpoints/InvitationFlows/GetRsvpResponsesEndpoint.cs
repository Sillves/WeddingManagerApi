using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.InvitationFlows;

public class GetRsvpResponsesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/rsvp-responses",
                async (Guid weddingId, Guid? flowId, IRsvpService rsvpService) =>
                {
                    var result = await rsvpService.GetResponsesAsync(weddingId, flowId);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("InvitationFlows")
            .WithName("GetRsvpResponses")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces<IEnumerable<RsvpResponseDto>>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
