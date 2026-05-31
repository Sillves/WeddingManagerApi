using System.Text.Json;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class RsvpFlowSubmitRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Dietary { get; set; }

    public RsvpResponseStatus Status { get; set; }
    public List<Guid> AttendingEventIds { get; set; } = new();

    public bool PlusOneAttending { get; set; }
    public string? PlusOneName { get; set; }
    public string? PlusOneDietary { get; set; }

    /// <summary>Keyed by question id. Value shape depends on question type.</summary>
    public Dictionary<string, JsonElement> CustomAnswers { get; set; } = new();
}
