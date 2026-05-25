using System.Text.Json;

namespace JamaConnect.Domain.Models;

public sealed record ItemTypeDefinition(
    int Id,
    string Name,
    IReadOnlyList<FieldDefinition> Fields,
    JsonElement? Raw);

public sealed record FieldDefinition(
    string Name,
    string? Label,
    string? FieldType,
    bool? Required,
    int? PickListId,
    JsonElement? Raw);

public sealed record RelationshipTypeDefinition(
    int Id,
    string Name,
    JsonElement? Raw);
