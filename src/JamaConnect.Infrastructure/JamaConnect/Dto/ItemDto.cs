using System.Text.Json.Serialization;

namespace JamaConnect.Infrastructure.JamaConnect.Dto;

internal sealed class ItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("documentKey")]
    public string? DocumentKey { get; init; }

    [JsonPropertyName("itemType")]
    public int ItemType { get; init; }

    [JsonPropertyName("project")]
    public int Project { get; init; }

    [JsonPropertyName("parent")]
    public int? Parent { get; init; }

    [JsonPropertyName("fields")]
    public ItemFields? Fields { get; init; }

    [JsonPropertyName("location")]
    public ItemLocation? Location { get; init; }
}

internal sealed class ItemFields
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

internal sealed class ItemLocation
{
    [JsonPropertyName("sequence")]
    public string? Sequence { get; init; }
}
