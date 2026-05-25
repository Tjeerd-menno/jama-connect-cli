using System.Text.Json;

namespace JamaConnect.Domain.Models;

public sealed record PageRequest(int StartAt = 0, int MaxResults = 50, int? Limit = null);

public sealed record ItemIdentifier(int? Id, string? DocumentKey)
{
    public static ItemIdentifier Parse(string value)
    {
        return int.TryParse(value, out var id)
            ? new ItemIdentifier(id, null)
            : new ItemIdentifier(null, value);
    }
}

public sealed record ItemQueryOptions(IReadOnlyList<string> Includes, bool IncludeRaw = true);

public sealed record ItemSearchCriteria(
    int? ProjectId,
    string? Type,
    string? DocumentKey,
    string? Contains,
    DateTimeOffset? CreatedSince,
    DateTimeOffset? ModifiedSince,
    bool RootOnly,
    IReadOnlyList<string> Includes);

public sealed record CreateItemRequest(
    int ProjectId,
    int ItemTypeId,
    int? ParentId,
    IReadOnlyDictionary<string, JsonElement> Fields,
    string? GlobalId = null);

public sealed record JsonPatchOperation(string Op, string Path, JsonElement? Value);

public sealed record RelationshipQuery(int? ItemId, string Direction, IReadOnlyList<string> Includes);

public sealed record CreateRelationshipRequest(int FromItemId, int ToItemId, int RelationshipTypeId);

public sealed record TestRunQuery(int? PlanId, int? CycleId, int? TestCaseId);

public sealed record JsonElementBackedVersion(int Id, JsonElement Raw);

public sealed record TestRunStepUpdate(int Index, string? Status, string? ActualResult);
