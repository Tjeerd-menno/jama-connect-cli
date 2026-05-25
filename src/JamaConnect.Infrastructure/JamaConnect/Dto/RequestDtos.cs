using System.Text.Json;
using System.Text.Json.Serialization;

namespace JamaConnect.Infrastructure.JamaConnect.Dto;

internal sealed record RequestItemDto(
    [property: JsonPropertyName("project")] int Project,
    [property: JsonPropertyName("itemType")] int ItemType,
    [property: JsonPropertyName("location")] RequestItemLocationDto? Location,
    [property: JsonPropertyName("globalId")] string? GlobalId,
    [property: JsonPropertyName("fields")] IReadOnlyDictionary<string, JsonElement> Fields);

internal sealed record RequestItemLocationDto(
    [property: JsonPropertyName("parent")] int Parent);

internal sealed record RequestRelationshipDto(
    [property: JsonPropertyName("fromItem")] int FromItem,
    [property: JsonPropertyName("toItem")] int ToItem,
    [property: JsonPropertyName("relationshipType")] int RelationshipType);

internal sealed record RequestTestCaseDto(
    [property: JsonPropertyName("testCase")] int TestCase);

internal sealed record PatchDto(
    [property: JsonPropertyName("op")] string Op,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("value")] JsonElement? Value);
