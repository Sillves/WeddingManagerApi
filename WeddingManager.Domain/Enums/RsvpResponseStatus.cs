using System.Text.Json.Serialization;

namespace WeddingManager.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RsvpResponseStatus
{
    Attending,
    Declined
}
