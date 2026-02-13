using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Authorization;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Invitations;

public class CreateInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/weddings/{weddingId}/invitations",
            async (Guid weddingId, CreateWeddingInvitationRequestDto requestDto,
                   IWeddingInvitationService invitationService) =>
            {
                var result = await invitationService.CreateInvitationAsync(weddingId, requestDto);
                if (!result.IsSuccess)
                    return result.ToErrorResult();
                return Results.Created(
                    $"/api/weddings/{weddingId}/invitations/{result.Value!.Id}", result.Value);
            })
            .WithTags("Invitations")
            .WithName("CreateInvitation")
            .RequireAuthorization()
            .AddEndpointFilter<RequireWeddingAccessFilter>()
            .RequireOwnerAccess()
            .Produces<WeddingInvitationDto>(201)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(403)
            .Produces<ErrorResponse>(409);
    }
}
