using JamaConnect.Domain.Interfaces;

namespace JamaConnect.Application.Configuration;

public sealed class AliasResolver
{
    private readonly JamaCliConfiguration _configuration;

    public AliasResolver(JamaCliConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JamaProfile ResolveProfile(string? profile)
    {
        var profileName = string.IsNullOrWhiteSpace(profile) ? _configuration.DefaultProfile : profile;
        if (!_configuration.Profiles.TryGetValue(profileName, out var resolved))
        {
            throw new InvalidOperationException($"Profile '{profileName}' is not configured.");
        }

        return resolved;
    }

    public int ResolveItemTypeId(string aliasOrId)
    {
        if (int.TryParse(aliasOrId, out var id))
        {
            return id;
        }

        if (_configuration.Aliases.ItemTypes.TryGetValue(aliasOrId, out var alias))
        {
            return alias.ItemTypeId;
        }

        throw new InvalidOperationException($"Item type alias '{aliasOrId}' is not configured.");
    }

    public int ResolveRelationshipTypeId(string aliasOrId)
    {
        if (int.TryParse(aliasOrId, out var id))
        {
            return id;
        }

        if (_configuration.Aliases.Relationships.TryGetValue(aliasOrId, out var alias))
        {
            return alias.RelationshipTypeId;
        }

        throw new InvalidOperationException($"Relationship alias '{aliasOrId}' is not configured.");
    }

    public IReadOnlyDictionary<string, ItemTypeAlias> ItemTypeAliases => _configuration.Aliases.ItemTypes;

    public IReadOnlyDictionary<string, RelationshipAlias> RelationshipAliases => _configuration.Aliases.Relationships;
}
