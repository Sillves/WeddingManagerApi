using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WeddingManager.Domain.Models;

namespace WeddingManager.Infrastructure.Data.Configurations;

/// <summary>
/// Builds EF value converters + comparers that store strongly-typed collections as jsonb,
/// using the shared <see cref="RsvpJson.Options"/> so storage matches the API payloads.
/// </summary>
internal static class JsonbConverters
{
    public static ValueConverter<List<T>, string> ListConverter<T>() =>
        new(
            v => JsonSerializer.Serialize(v, RsvpJson.Options),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<T>()
                : JsonSerializer.Deserialize<List<T>>(v, RsvpJson.Options) ?? new List<T>());

    public static ValueComparer<List<T>> ListComparer<T>() =>
        new(
            (a, b) => JsonSerializer.Serialize(a, RsvpJson.Options) == JsonSerializer.Serialize(b, RsvpJson.Options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, RsvpJson.Options).GetHashCode(),
            v => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(v, RsvpJson.Options), RsvpJson.Options) ?? new List<T>());
}
