using Microsoft.AspNetCore.DataProtection;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Web.Extensions;
using WeddingManager.Web.Models;

namespace WeddingManager.Web.Endpoints.Rsvp;

public class SubmitRsvpFlowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/weddings/{slug}/rsvp/submit",
                async (string slug, RsvpFlowSubmitRequestDto requestDto, HttpContext http,
                    IRsvpService rsvpService, IDataProtectionProvider dp) =>
                {
                    var flowId = RsvpFlowCookie.ResolveFlowId(http, dp);
                    if (flowId == null)
                    {
                        return Results.Json(
                            new ErrorResponse { Errors = [new("unauthorized", "Unlock the RSVP form before submitting.")] },
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    var result = await rsvpService.SubmitAsync(slug, flowId.Value, requestDto);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
                })
            .WithTags("Rsvp")
            .WithName("SubmitRsvpFlow")
            .AllowAnonymous()
            .RequireRateLimiting("PublicApi")
            .Produces<RsvpSubmitResultDto>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ErrorResponse>(401)
            .Produces<ErrorResponse>(404)
            .Produces<ErrorResponse>(409)
            .Produces<ErrorResponse>(429);
    }
}
