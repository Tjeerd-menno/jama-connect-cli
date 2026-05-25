using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using JamaConnect.Application.Output;
using JamaConnect.Domain.Models;

namespace JamaConnect.Cli.Commands;

internal sealed class CliRuntime
{
    public const string ApiVersion = "jama-connect-cli/v1";

    public CliRuntime(
        Option<string> format,
        Option<string> profile,
        Option<bool> quiet,
        Option<bool> verbose,
        Option<bool> noColor,
        Option<bool> dryRun,
        Option<bool> agent,
        Option<string> errorFormat)
    {
        Format = format;
        Profile = profile;
        Quiet = quiet;
        Verbose = verbose;
        NoColor = noColor;
        DryRun = dryRun;
        Agent = agent;
        ErrorFormat = errorFormat;
    }

    public Option<string> Format { get; }

    public Option<string> Profile { get; }

    public Option<bool> Quiet { get; }

    public Option<bool> Verbose { get; }

    public Option<bool> NoColor { get; }

    public Option<bool> DryRun { get; }

    public Option<bool> Agent { get; }

    public Option<string> ErrorFormat { get; }

    public CliContext Create(ParseResult parseResult)
    {
        var agent = parseResult.GetValue(Agent);
        return new CliContext(
            agent ? "json" : parseResult.GetValue(Format) ?? "table",
            parseResult.GetValue(Profile) ?? "default",
            parseResult.GetValue(Quiet) || agent,
            parseResult.GetValue(Verbose),
            parseResult.GetValue(NoColor) || agent,
            parseResult.GetValue(DryRun),
            agent,
            agent ? "json" : parseResult.GetValue(ErrorFormat) ?? "text");
    }
}

internal sealed record CliContext(
    string Format,
    string Profile,
    bool Quiet,
    bool Verbose,
    bool NoColor,
    bool DryRun,
    bool Agent,
    string ErrorFormat);

internal static class CliOutput
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static void WriteResult<T>(CliContext context, string kind, T result, IReadOnlyList<string>? warnings = null)
    {
        if (context.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            WriteEnvelope(kind, context.Profile, "result", result, warnings ?? []);
            return;
        }

        WriteTable(result);
    }

    public static void WritePage<T>(CliContext context, string kind, JamaPage<T> page, IReadOnlyList<string>? warnings = null)
    {
        if (context.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            WriteListEnvelope(kind, context.Profile, page, warnings ?? []);
            return;
        }

        if (context.Format.Equals("ndjson", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var item in page.Data)
            {
                Console.WriteLine(JsonSerializer.Serialize(item, JsonOptions));
            }

            return;
        }

        if (context.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            WriteCsv(page.Data);
            return;
        }

        WriteTable(page.Data);
    }

    public static void WriteError(CliContext context, string code, string message, object? details = null, bool retryable = false)
    {
        if (context.ErrorFormat.Equals("json", StringComparison.OrdinalIgnoreCase) || context.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            WriteErrorEnvelope(code, message, details, retryable);
            return;
        }

        Console.Error.WriteLine($"{code}: {message}");
    }

    private static void WriteEnvelope<T>(string kind, string profile, string resultProperty, T result, IReadOnlyList<string> warnings)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", kind);
            writer.WriteString("apiVersion", CliRuntime.ApiVersion);
            writer.WriteString("profile", profile);
            writer.WritePropertyName(resultProperty);
            WriteJsonValue(writer, result);
            WriteStringArray(writer, "warnings", warnings);
            writer.WritePropertyName("links");
            writer.WriteStartArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        Console.WriteLine(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void WriteListEnvelope<T>(string kind, string profile, JamaPage<T> page, IReadOnlyList<string> warnings)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", kind);
            writer.WriteString("apiVersion", CliRuntime.ApiVersion);
            writer.WriteString("profile", profile);
            writer.WritePropertyName("page");
            writer.WriteStartObject();
            writer.WriteNumber("startAt", page.StartAt);
            writer.WriteNumber("maxResults", page.MaxResults);
            writer.WriteNumber("resultCount", page.ResultCount);
            writer.WriteNumber("totalResults", page.TotalResults);
            writer.WriteBoolean("fetchedAll", page.FetchedAll);
            writer.WriteEndObject();
            writer.WritePropertyName("data");
            writer.WriteStartArray();
            foreach (var item in page.Data)
            {
                WriteJsonValue(writer, item);
            }

            writer.WriteEndArray();
            WriteStringArray(writer, "warnings", warnings);
            writer.WritePropertyName("links");
            writer.WriteStartArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        Console.WriteLine(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void WriteErrorEnvelope(string code, string message, object? details, bool retryable)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", "error");
            writer.WriteString("apiVersion", CliRuntime.ApiVersion);
            writer.WriteString("code", code);
            writer.WriteString("message", message);
            writer.WritePropertyName("details");
            WriteJsonValue(writer, details);
            writer.WriteBoolean("retryable", retryable);
            writer.WriteEndObject();
        }

        Console.Error.WriteLine(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void WriteStringArray(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static void WriteJsonValue<T>(Utf8JsonWriter writer, T value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value is JsonElement element)
        {
            element.WriteTo(writer);
            return;
        }

        JsonSerializer.Serialize(writer, value, JsonOptions);
    }

    private static void WriteTable<T>(T value)
    {
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            var rows = enumerable.Cast<object?>().Select(ToJsonElement).ToArray();
            if (rows.Length == 0)
            {
                return;
            }

            var properties = GetSimplePropertyNames(rows[0])
                .Take(6)
                .ToArray();
            if (properties.Length > 0)
            {
                Console.WriteLine(string.Join("  ", properties));
                Console.WriteLine(new string('-', Math.Min(100, Math.Max(20, properties.Sum(x => x.Length + 2)))));
                foreach (var row in rows)
                {
                    Console.WriteLine(string.Join("  ", properties.Select(x => row.TryGetProperty(x, out var property) ? FormatJsonCell(property) : string.Empty)));
                }

                return;
            }

            foreach (var item in rows)
            {
                Console.WriteLine(item.GetRawText());
            }

            return;
        }

        Console.WriteLine(JsonSerializer.Serialize(value, JsonOptions));
    }

    private static void WriteCsv<T>(IReadOnlyList<T> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        var rows = values.Select(ToJsonElement).ToArray();
        var properties = GetSimplePropertyNames(rows[0]).ToArray();
        if (properties.Length == 0)
        {
            foreach (var value in rows)
            {
                Console.WriteLine(EscapeCsv(value.GetRawText()));
            }

            return;
        }

        Console.WriteLine(string.Join(",", properties.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            Console.WriteLine(string.Join(",", properties.Select(x => EscapeCsv(row.TryGetProperty(x, out var property) ? FormatJsonCell(property) : string.Empty))));
        }
    }

    private static JsonElement ToJsonElement<T>(T value) => JsonSerializer.SerializeToElement(value, JsonOptions);

    private static IEnumerable<string> GetSimplePropertyNames(JsonElement row)
    {
        return row.ValueKind == JsonValueKind.Object
            ? row.EnumerateObject().Where(x => IsSimpleJsonValue(x.Value)).Select(x => x.Name)
            : [];
    }

    private static bool IsSimpleJsonValue(JsonElement value)
    {
        return value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null;
    }

    private static string FormatJsonCell(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            JsonValueKind.Null => string.Empty,
            _ => value.GetRawText()
        };
    }

    private static string EscapeCsv(string value)
    {
        return value.Contains('"', StringComparison.Ordinal) || value.Contains(',', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal)
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }
}
