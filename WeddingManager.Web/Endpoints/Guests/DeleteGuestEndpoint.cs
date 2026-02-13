using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Guests;

public class DeleteGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/guests/{guestId}", 
            async (Guid guestId, IGuestService guestService) =>
            {
                var result = await guestService.DeleteGuestAsync(guestId);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
            .WithTags("Guests")
            .WithName("DeleteGuest")
            .RequireAuthorization()
            .AddEndpointFilter<RequireGuestAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces(204)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
