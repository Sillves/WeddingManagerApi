using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Invitations;

public class AcceptInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/invitations/{token}/accept",
            async (string token, IWeddingInvitationService invitationService, IUserContextService userContextService) =>
            {
                var userId = userContextService.GetUserId();
                var result = await invitationService.AcceptInvitationAsync(token, userId);
                return result.IsSuccess ? Results.Ok() : result.ToErrorResult();
            })
            .WithTags("Invitations")
            .WithName("AcceptInvitation")
            .RequireAuthorization()
            .Produces(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(409);
    }
}
