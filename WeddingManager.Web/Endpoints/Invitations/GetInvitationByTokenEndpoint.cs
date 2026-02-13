using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Invitations;

public class GetInvitationByTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/invitations/{token}",
            async (string token, IWeddingInvitationService invitationService) =>
            {
                var result = await invitationService.GetByTokenAsync(token);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("Invitations")
            .WithName("GetInvitationByToken")
            .Produces<WeddingInvitationDto>(200)
            .Produces<ErrorResponse>(404);
    }
}
