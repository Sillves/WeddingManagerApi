using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Referrals;

public class GetReferralsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/referrals",
            async (IReferralService referralService, IUserContextService userContextService) =>
            {
                var userId = userContextService.GetUserId();
                var result = await referralService.GetReferralsAsync(userId);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("Referrals")
            .WithName("GetReferrals")
            .RequireAuthorization()
            .Produces<IEnumerable<ReferralDto>>(200)
            .Produces<ErrorResponse>(401);
    }
}
