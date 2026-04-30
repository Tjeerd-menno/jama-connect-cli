namespace JamaConnect.Infrastructure.Options;

public sealed class JamaConnectOptions
{
    public const string SectionName = "JamaConnect";

    public required string BaseUrl { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string TokenEndpoint { get; init; } = "/rest/oauth/token";
    public int TimeoutSeconds { get; init; } = 30;
}
