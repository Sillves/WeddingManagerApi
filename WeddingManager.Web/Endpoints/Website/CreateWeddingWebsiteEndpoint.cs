using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Website;

public class CreateWeddingWebsiteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/website",
                async (Guid weddingId, CreateWeddingWebsiteRequestDto request,
                    IWeddingWebsiteService websiteService, IUserContextService userContext) =>
                {
                    var userId = userContext.GetUserId();
                    var result = await websiteService.CreateAsync(userId, weddingId, request);

                    if (!result.IsSuccess)
                    {
                        return result.ToErrorResult();
                    }

                    return Results.Created($"/api/weddings/{weddingId}/website", result.Value);
                })
            .WithTags("Website")
            .WithName("CreateWeddingWebsite")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<WeddingWebsiteDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(409);
    }
}
