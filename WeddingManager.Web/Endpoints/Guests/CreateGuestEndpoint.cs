using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Guests;

public class CreateGuestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests", 
            async (Guid weddingId, CreateGuestRequestDto requestDto, IGuestService guestService) =>
            {
                var result = await guestService.CreateGuestAsync(weddingId, requestDto);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                var guest = result.Value!;
                return Results.Created($"/api/guests/{guest.Id}", guest);
            })
            .WithTags("Guests")
            .WithName("CreateGuest")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireModuleAccess(WeddingModule.Guests)
            .Produces<GuestDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(500);
    }
}
