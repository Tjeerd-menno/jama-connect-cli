using System.CommandLine;
using System.Text.Json;
using JamaConnect.Domain.Models;

namespace JamaConnect.Cli.Commands;

internal static class CommandHelpers
{
    public static Option<int?> ProjectOption() => new("--project", "-p")
    {
        Description = "Project ID."
    };

    public static Option<bool> AllOption() => new("--all")
    {
        Description = "Fetch all pages."
    };

    public static Option<int> PageSizeOption() => new("--page-size")
    {
        Description = "Page size, capped at 50.",
        DefaultValueFactory = _ => 50
    };

    public static Option<int> StartAtOption() => new("--start-at")
    {
        Description = "Zero-based start offset.",
        DefaultValueFactory = _ => 0
    };

    public static Option<int?> LimitOption() => new("--limit")
    {
        Description = "Maximum number of records to fetch."
    };

    public static PageRequest Page(ParseResult parseResult, Option<int> startAt, Option<int> pageSize, Option<int?> limit)
        => new(parseResult.GetValue(startAt), Math.Min(parseResult.GetValue(pageSize), 50), parseResult.GetValue(limit));

    public static IReadOnlyList<string> SplitCsv(string? value)
        => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static Dictionary<string, JsonElement> ReadFields(IEnumerable<string> values)
    {
        var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var parts = value.Split('=', 2);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Field '{value}' must use name=value syntax.");
            }

            fields[parts[0]] = StringElement(ReadTextValue(parts[1]));
        }

        return fields;
    }

    public static string? ReadOptionalTextValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? value : ReadTextValue(value);

    public static string ReadTextValue(string value)
    {
        if (value == "-")
        {
            return Console.In.ReadToEnd();
        }

        return value.Length > 1 && value[0] == '@'
            ? File.ReadAllText(value[1..])
            : value;
    }

    public static JsonElement ReadJsonObject(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return JsonElement.Parse("{}");
        }

        var text = input == "-"
            ? Console.In.ReadToEnd()
            : File.ReadAllText(input);
        return JsonElement.Parse(text);
    }

    public static JsonElement DryRunPlan(params JsonElement[] operations)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("operations");
            writer.WriteStartArray();
            foreach (var operation in operations)
            {
                operation.WriteTo(writer);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    public static JsonElement DryRunOperation(string method, string resource, string description, JsonElement? bodyPreview = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("method", method);
            writer.WriteString("resource", resource);
            writer.WriteString("description", description);
            if (bodyPreview is not null)
            {
                writer.WritePropertyName("bodyPreview");
                bodyPreview.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    public static JsonElement ObjectPreview(params (string Name, object? Value)[] properties)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Name);
                WritePreviewValue(writer, property.Value);
            }

            writer.WriteEndObject();
        }

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    public static JsonElement StringElement(string? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStringValue(value);
        }

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    public static JsonElement PatchPreview(IReadOnlyList<JsonPatchOperation> operations)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            foreach (var operation in operations)
            {
                writer.WriteStartObject();
                writer.WriteString("op", operation.Op);
                writer.WriteString("path", operation.Path);
                writer.WritePropertyName("value");
                if (operation.Value is { } value)
                {
                    value.WriteTo(writer);
                }
                else
                {
                    writer.WriteNullValue();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private static void WritePreviewValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
