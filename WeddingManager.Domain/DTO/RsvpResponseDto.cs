using System.Text.Json;
using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.DTO;

public class RsvpResponseDto
{
    public Guid Id { get; set; }
    public Guid InvitationFlowId { get; set; }
    public string FlowName { get; set; } = string.Empty;
    public Guid GuestId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Dietary { get; set; }
    public RsvpResponseStatus Status { get; set; }
    public List<Guid> AttendingEventIds { get; set; } = new();
    public List<string> AttendingEventNames { get; set; } = new();
    public Dictionary<string, JsonElement> CustomAnswers { get; set; } = new();
    public bool IsPlusOne { get; set; }
    public Guid? PlusOneOfGuestId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
