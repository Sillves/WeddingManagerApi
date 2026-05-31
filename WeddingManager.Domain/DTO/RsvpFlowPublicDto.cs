using WeddingManager.Domain.Models;

namespace WeddingManager.Domain.DTO;

/// <summary>What a guest sees once a flow is unlocked. Never exposes passcode or planner label.</summary>
public class RsvpFlowPublicDto
{
    public Guid FlowId { get; set; }
    public Guid WeddingId { get; set; }
    public bool IncludePlusOne { get; set; }
    public List<QuestionDefinition> Questions { get; set; } = new();

    /// <summary>Merged real + custom events, sorted by start date.</summary>
    public List<RsvpEventOptionDto> Events { get; set; } = new();
}
