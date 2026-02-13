using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Invitations;

public class GetInvitationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weddings/{weddingId}/invitations",
            async (Guid weddingId, IWeddingInvitationService invitationService) =>
            {
                var result = await invitationService.GetInvitationsAsync(weddingId);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("Invitations")
            .WithName("GetInvitations")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireOwnerAccess()
            .Produces<IEnumerable<WeddingInvitationDto>>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403);
    }
}
