using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Weddings;

public class UpdateWedding : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/weddings/{id:guid}", async (Guid id, UpdateWeddingRequestDto requestDto, IWeddingService weddingService, ApplicationMapper mapper) =>
        {
            // Get existing wedding
            var existingResult = await weddingService.GetByIdAsync(id);
            if (!existingResult.IsSuccess)
            {
                return existingResult.ToErrorResult();
            }

            var wedding = existingResult.Value!;

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(requestDto.Title))
            {
                wedding.Title = requestDto.Title;
            }

            if (requestDto.Date.HasValue)
            {
                wedding.Date = requestDto.Date.Value;
            }

            if (!string.IsNullOrWhiteSpace(requestDto.Location))
            {
                wedding.Location = requestDto.Location;
            }

            var result = await weddingService.UpdateAsync(wedding);
            if (!result.IsSuccess)
            {
                return result.ToErrorResult();
            }

            return Results.Ok(mapper.WeddingToDto(wedding));
        })
        .WithTags("Weddings")
        .WithName("UpdateWedding")
        .RequireAuthorization()
        .Produces<WeddingDto>(200)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(401)
        .Produces<ErrorResponse>(404);
    }
}
