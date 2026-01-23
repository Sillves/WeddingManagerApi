using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeddingManager.Web.Json;

public sealed class BoolStringJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => ReadString(reader),
            _ => throw new JsonException("Invalid boolean value.")
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);

    private static bool ReadString(Utf8JsonReader reader)
    {
        var value = reader.GetString();
        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        throw new JsonException($"Invalid boolean value: '{value}'.");
    }
}
