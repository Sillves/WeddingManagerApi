using WeddingManager.Domain.Models;

namespace WeddingManager.Web.Models;

public class ErrorResponse
{
    public IReadOnlyList<Error> Errors { get; set; } = Array.Empty<Error>();
}
