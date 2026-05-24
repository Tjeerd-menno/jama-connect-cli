using System.Text.Json;

namespace JamaConnect.Infrastructure.JamaConnect;

internal static class JsonElementExtensions
{
    public static int GetInt(this JsonElement element, string name, int fallback = 0)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : fallback;
    }

    public static int? GetNullableInt(this JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : null;
    }

    public static string? GetStringOrNull(this JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    public static bool GetBool(this JsonElement element, string name, bool fallback = false)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : fallback;
    }

    public static JsonElement? GetObject(this JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : null;
    }

    public static Dictionary<string, JsonElement> ToDictionary(this JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        return element.Value.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.OrdinalIgnoreCase);
    }
}
