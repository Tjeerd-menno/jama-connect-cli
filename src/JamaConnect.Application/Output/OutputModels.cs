namespace JamaConnect.Application.Output;

public sealed record JsonEnvelope<T>(
    string Kind,
    string ApiVersion,
    string Profile,
    T? Result,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<LinkModel> Links);

public sealed record JsonListEnvelope<T>(
    string Kind,
    string ApiVersion,
    string Profile,
    PageModel Page,
    IReadOnlyList<T> Data,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<LinkModel> Links);

public sealed record PageModel(
    int StartAt,
    int MaxResults,
    int ResultCount,
    int TotalResults,
    bool FetchedAll);

public sealed record LinkModel(string Rel, string Href);

public sealed record ErrorEnvelope(
    string Kind,
    string ApiVersion,
    string Code,
    string Message,
    object? Details,
    bool Retryable);
