using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Website;

public class DeleteWeddingWebsiteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/weddings/{weddingId}/website",
                async (Guid weddingId, IWeddingWebsiteService websiteService) =>
                {
                    var result = await websiteService.DeleteAsync(weddingId);
                    return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
                })
            .WithTags("Website")
            .WithName("DeleteWeddingWebsite")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Website)
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
