using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Guests;

public class ImportGuestsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/guests/import",
            async (Guid weddingId, BulkImportGuestRequestDto requestDto, IGuestService guestService) =>
            {
                var result = await guestService.ImportGuestsAsync(weddingId, requestDto);
                if (!result.IsSuccess)
                {
                    return result.ToErrorResult();
                }

                return Results.Ok(result.Value);
            })
            .WithTags("Guests")
            .WithName("ImportGuests")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .Produces<BulkImportGuestResultDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(500);
    }
}
