using System.Text.Json;
using System.Text.Json.Serialization;

namespace JamaConnect.Infrastructure.JamaConnect.Dto;

internal sealed class JsonPagedResponse
{
    [JsonPropertyName("meta")]
    public JsonMeta? Meta { get; init; }

    [JsonPropertyName("data")]
    public JsonElement[]? Data { get; init; }
}

internal sealed class JsonSingleResponse
{
    [JsonPropertyName("meta")]
    public JsonMeta? Meta { get; init; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
}

internal sealed class JsonMeta
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("pageInfo")]
    public JsonPageInfo? PageInfo { get; init; }
}

internal sealed class JsonPageInfo
{
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; init; }

    [JsonPropertyName("resultCount")]
    public int ResultCount { get; init; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; init; }
}

internal sealed class JsonErrorResponse
{
    [JsonPropertyName("meta")]
    public JsonMeta? Meta { get; init; }

    [JsonPropertyName("errors")]
    public JsonElement[]? Errors { get; init; }
}
