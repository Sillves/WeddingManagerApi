
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Guests;

public class GetGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/guests/{guestId}", 
            async (Guid guestId, IGuestService guestService) =>
            {
                var result = await guestService.GetByIdAsync(guestId);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("Guests")
            .WithName("GetGuest")
            .RequireAuthorization()
            .AddEndpointFilter<RequireGuestAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces<GuestDto>(200)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(404);
    }
}
