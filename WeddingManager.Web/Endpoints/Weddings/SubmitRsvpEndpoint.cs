using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class SubmitRsvpEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{id}/rsvp",
            async (Guid id, RsvpSubmitRequestDto requestDto, IGuestService guestService) =>
            {
                var result = await guestService.SubmitRsvpAsync(id, requestDto);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .WithTags("Weddings")
            .WithName("SubmitRsvp")
            .Produces<GuestDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(404);
    }
}
