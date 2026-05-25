using System.Text.Json;
using JamaConnect.Application.Configuration;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Relationships;

public sealed class RelationshipUseCases
{
    private readonly IRelationshipReader _reader;
    private readonly IRelationshipWriter _writer;
    private readonly AliasResolver _aliases;

    public RelationshipUseCases(IRelationshipReader reader, IRelationshipWriter writer, AliasResolver aliases)
    {
        _reader = reader;
        _writer = writer;
        _aliases = aliases;
    }

    public Task<JamaPage<JamaRelationship>> ListAsync(RelationshipQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        return _reader.GetRelationshipsAsync(query, page, cancellationToken);
    }

    public Task<JamaRelationship?> GetAsync(int id, CancellationToken cancellationToken = default)
        => _reader.GetRelationshipAsync(id, cancellationToken);

    public Task<JsonElement> CreateAsync(int fromItemId, int toItemId, string type, CancellationToken cancellationToken = default)
    {
        return _writer.CreateRelationshipAsync(new CreateRelationshipRequest(fromItemId, toItemId, _aliases.ResolveRelationshipTypeId(type)), cancellationToken);
    }

    public RelationshipValidationResult Validate(int fromItemId, int toItemId, string type)
    {
        var relationshipTypeId = _aliases.ResolveRelationshipTypeId(type);
        var warnings = new List<string>
        {
            "Local validation resolved the relationship type. Jama still performs server-side rule validation on create."
        };

        if (!_aliases.RelationshipAliases.TryGetValue(type, out var alias) || (string.IsNullOrWhiteSpace(alias.From) && string.IsNullOrWhiteSpace(alias.To)))
        {
            warnings.Add("No configured source/target aliases are available for direction validation.");
        }

        return new RelationshipValidationResult(true, fromItemId, toItemId, relationshipTypeId, warnings);
    }

    public Task<JsonElement> UpdateAsync(int id, JsonElement request, CancellationToken cancellationToken = default)
        => _writer.UpdateRelationshipAsync(id, request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) => _writer.DeleteRelationshipAsync(id, cancellationToken);

    public Task ClearSuspectAsync(int id, CancellationToken cancellationToken = default) => _writer.ClearSuspectAsync(id, cancellationToken);
}

public sealed record RelationshipValidationResult(
    bool Valid,
    int FromItemId,
    int ToItemId,
    int RelationshipTypeId,
    IReadOnlyList<string> Warnings);
