using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Referrals;

public class GetReferralStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/referrals/stats",
            async (IReferralService referralService, IUserContextService userContextService) =>
            {
                var userId = userContextService.GetUserId();
                var result = await referralService.GetStatsAsync(userId);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("Referrals")
            .WithName("GetReferralStats")
            .RequireAuthorization()
            .Produces<ReferralStatsDto>(200)
            .Produces<ErrorResponse>(401);
    }
}
