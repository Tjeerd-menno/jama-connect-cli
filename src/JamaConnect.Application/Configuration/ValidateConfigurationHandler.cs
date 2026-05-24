using JamaConnect.Domain.Interfaces;

namespace JamaConnect.Application.Configuration;

public sealed class ValidateConfigurationHandler
{
    private readonly JamaCliConfiguration _configuration;
    private readonly ISchemaReader _schemaReader;

    public ValidateConfigurationHandler(JamaCliConfiguration configuration, ISchemaReader schemaReader)
    {
        _configuration = configuration;
        _schemaReader = schemaReader;
    }

    public async Task<ConfigurationValidationResult> HandleAsync(string profile, int? projectId, CancellationToken cancellationToken = default)
    {
        var itemTypes = await _schemaReader.GetItemTypesAsync(new Domain.Models.PageRequest(), cancellationToken).ConfigureAwait(false);
        var relationshipTypes = await _schemaReader.GetRelationshipTypesAsync(new Domain.Models.PageRequest(), cancellationToken).ConfigureAwait(false);
        var itemTypeIds = itemTypes.Data.Select(x => x.Id).ToHashSet();
        var relationshipTypeIds = relationshipTypes.Data.Select(x => x.Id).ToHashSet();

        var itemAliases = _configuration.Aliases.ItemTypes
            .Select(x => new AliasValidation(x.Key, x.Value.ItemTypeId, itemTypeIds.Contains(x.Value.ItemTypeId)))
            .ToArray();
        var relationshipAliases = _configuration.Aliases.Relationships
            .Select(x => new AliasValidation(x.Key, x.Value.RelationshipTypeId, relationshipTypeIds.Contains(x.Value.RelationshipTypeId)))
            .ToArray();
        var warnings = itemAliases.Concat(relationshipAliases).Where(x => !x.Valid).Select(x => $"Alias '{x.Alias}' points to missing id {x.Id}.").ToArray();

        return new ConfigurationValidationResult(
            itemAliases.All(x => x.Valid) && relationshipAliases.All(x => x.Valid),
            profile,
            projectId,
            itemAliases,
            relationshipAliases,
            warnings);
    }
}

public sealed record ConfigurationValidationResult(
    bool Valid,
    string Profile,
    int? Project,
    IReadOnlyList<AliasValidation> ItemTypeAliases,
    IReadOnlyList<AliasValidation> RelationshipAliases,
    IReadOnlyList<string> Warnings);

public sealed record AliasValidation(string Alias, int Id, bool Valid);
