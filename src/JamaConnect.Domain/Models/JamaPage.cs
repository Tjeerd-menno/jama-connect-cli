namespace JamaConnect.Domain.Models;

public sealed record JamaPage<T>(
    int StartAt,
    int MaxResults,
    int ResultCount,
    int TotalResults,
    IReadOnlyList<T> Data)
{
    public bool FetchedAll => StartAt + ResultCount >= TotalResults;
}
