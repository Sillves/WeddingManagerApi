using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.DTO;

public class UpdateInvitationFlowRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Passcode { get; set; }
    public bool IncludePlusOne { get; set; }
    public List<QuestionDefinition> CustomQuestions { get; set; } = new();
    public List<Guid> EventIds { get; set; } = new();
    public List<CustomEventDefinition> CustomEvents { get; set; } = new();
}
