using System.Text.Json.Serialization;

namespace JamaConnect.Infrastructure.JamaConnect.Dto;

internal sealed class PagedResponse<T>
{
    [JsonPropertyName("meta")]
    public Meta? Meta { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyList<T>? Data { get; init; }
}

internal sealed class SingleResponse<T>
{
    [JsonPropertyName("meta")]
    public Meta? Meta { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

internal sealed class Meta
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("pageInfo")]
    public PageInfo? PageInfo { get; init; }
}

internal sealed class PageInfo
{
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; init; }

    [JsonPropertyName("resultCount")]
    public int ResultCount { get; init; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; init; }
}

internal sealed class ProjectDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("projectKey")]
    public string? ProjectKey { get; init; }

    [JsonPropertyName("isFolder")]
    public bool IsFolder { get; init; }

    [JsonPropertyName("fields")]
    public ProjectFields? Fields { get; init; }
}

internal sealed class ProjectFields
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
