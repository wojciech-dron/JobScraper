using System.Text.Json;
using System.Text.Json.Nodes;

namespace JobScraper.IntegrationTests.Utils;

public static class JsonHelpers
{
    public static JsonSerializerOptions PrettyOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary> Serializes to pretty json </summary>
    public static string SerializePretty(this object? obj) => JsonSerializer.Serialize(obj, PrettyOptions);

    /// <summary> Serializes to raw json </summary>
    public static string Serialize(this object? obj) => JsonSerializer.Serialize(obj, Options);

    /// <summary> Deserializes json </summary>
    public static TValue Deserialize<TValue>(this string json) => JsonSerializer.Deserialize<TValue>(json)!;

    /// <summary> Deserializes json </summary>
    public static TValue Deserialize<TValue>(this JsonDocument json) => JsonSerializer.Deserialize<TValue>(json)!;

    /// <summary> Deserializes json </summary>
    public static TValue Deserialize<TValue>(this JsonElement json) => JsonSerializer.Deserialize<TValue>(json)!;

    /// <summary> Deserializes json </summary>
    public static TValue Deserialize<TValue>(this JsonNode json) => JsonSerializer.Deserialize<TValue>(json)!;
}
