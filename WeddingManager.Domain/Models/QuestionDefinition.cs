using WeddingManager.Domain.Enums;

namespace WeddingManager.Domain.Models;

/// <summary>
/// A custom question defined on an <see cref="Entities.InvitationFlow"/>, stored as jsonb.
/// </summary>
public class QuestionDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RsvpQuestionType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; }

    /// <summary>Choices for SingleChoice / MultiChoice; null or empty for YesNo / FreeText.</summary>
    public List<string>? Options { get; set; }
}
