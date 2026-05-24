using System.Runtime.CompilerServices;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Infrastructure.JamaConnect;

public sealed class JamaPaginator : IJamaPaginator
{
    public async IAsyncEnumerable<T> GetAllAsync<T>(
        Func<int, int, CancellationToken, Task<JamaPage<T>>> fetchPage,
        int pageSize,
        int? limit,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var startAt = 0;
        var fetched = 0;
        var maxResults = Math.Min(pageSize, 50);

        while (true)
        {
            var page = await fetchPage(startAt, maxResults, cancellationToken).ConfigureAwait(false);
            foreach (var item in page.Data)
            {
                if (limit is not null && fetched >= limit.Value)
                {
                    yield break;
                }

                fetched++;
                yield return item;
            }

            if (page.FetchedAll || page.ResultCount == 0)
            {
                yield break;
            }

            startAt += page.ResultCount;
        }
    }
}
