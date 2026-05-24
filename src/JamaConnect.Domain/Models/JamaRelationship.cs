using System.Text.Json;

namespace JamaConnect.Domain.Models;

public sealed record JamaRelationship(
    int Id,
    int RelationshipTypeId,
    string? RelationshipTypeAlias,
    int FromItemId,
    int ToItemId,
    bool Suspect,
    JsonElement? Raw);
