using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Invitations;

public class CancelInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/weddings/{weddingId}/invitations/{invitationId}",
            async (Guid weddingId, Guid invitationId, IWeddingInvitationService invitationService) =>
            {
                var result = await invitationService.CancelInvitationAsync(weddingId, invitationId);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
            .WithTags("Invitations")
            .WithName("CancelInvitation")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireOwnerAccess()
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
