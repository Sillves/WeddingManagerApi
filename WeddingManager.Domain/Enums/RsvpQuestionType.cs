using System.Text.Json.Serialization;

namespace WeddingManager.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RsvpQuestionType
{
    YesNo,
    FreeText,
    SingleChoice,
    MultiChoice
}
