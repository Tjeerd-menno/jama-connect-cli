using System.Text.Json;

namespace JamaConnect.Domain.Models;

public sealed record JamaItem(
    int Id,
    string? DocumentKey,
    string? GlobalId,
    int ProjectId,
    int ItemTypeId,
    string? ItemTypeAlias,
    int? ParentId,
    string Title,
    IReadOnlyDictionary<string, JsonElement> Fields,
    JsonElement? Raw);
