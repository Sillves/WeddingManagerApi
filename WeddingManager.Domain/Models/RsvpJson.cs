using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeddingManager.Domain.Models;

/// <summary>
/// Shared System.Text.Json options for RSVP jsonb columns and DTO payloads, so the EF value
/// converters and the service layer serialize identically (camelCase keys, string enums).
/// </summary>
public static class RsvpJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
