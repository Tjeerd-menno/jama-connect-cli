namespace JamaConnect.Application.Configuration;

public sealed class JamaCliConfiguration
{
    public const string SectionName = "JamaCli";

    public string DefaultProfile { get; init; } = "default";

    public Dictionary<string, JamaProfile> Profiles { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = new()
    };

    public AliasConfiguration Aliases { get; init; } = new();

    public List<TraceabilityRule> TraceabilityRules { get; init; } = [];

    public HttpPolicyConfiguration Http { get; init; } = new();
}

public sealed class JamaProfile
{
    public string? BaseUrl { get; init; }

    public int? Project { get; init; }

    public string Output { get; init; } = "table";

    public bool Production { get; init; }
}

public sealed class AliasConfiguration
{
    public Dictionary<string, ItemTypeAlias> ItemTypes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, RelationshipAlias> Relationships { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ItemTypeAlias
{
    public int ItemTypeId { get; init; }

    public string? DisplayName { get; init; }

    public Dictionary<string, string> RequiredFields { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RelationshipAlias
{
    public int RelationshipTypeId { get; init; }

    public string? From { get; init; }

    public string? To { get; init; }
}

public sealed class TraceabilityRule
{
    public string Name { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Relation { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;

    public string Direction { get; init; } = "downstream";

    public int MinTargets { get; init; } = 1;
}

public sealed class HttpPolicyConfiguration
{
    public int TimeoutSeconds { get; init; } = 30;

    public RetryPolicyConfiguration Retry { get; init; } = new();
}

public sealed class RetryPolicyConfiguration
{
    public int MaxAttempts { get; init; } = 5;

    public int InitialDelayMilliseconds { get; init; } = 250;

    public int MaxDelaySeconds { get; init; } = 10;

    public int[] RetryStatusCodes { get; init; } = [429, 502, 503, 504];
}
