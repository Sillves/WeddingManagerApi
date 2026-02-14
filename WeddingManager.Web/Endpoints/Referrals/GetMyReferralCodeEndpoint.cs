using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Referrals;

public class GetMyReferralCodeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/referrals/code",
            async (IReferralService referralService, IUserContextService userContextService) =>
            {
                var userId = userContextService.GetUserId();
                var result = await referralService.GetOrCreateReferralCodeAsync(userId);
                return result.IsSuccess ? Results.Ok(new { code = result.Value }) : result.ToErrorResult();
            })
            .WithTags("Referrals")
            .WithName("GetMyReferralCode")
            .RequireAuthorization()
            .Produces<object>(200)
            .Produces<ErrorResponse>(401);
    }
}
