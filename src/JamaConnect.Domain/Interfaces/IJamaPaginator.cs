using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IJamaPaginator
{
    IAsyncEnumerable<T> GetAllAsync<T>(
        Func<int, int, CancellationToken, Task<JamaPage<T>>> fetchPage,
        int pageSize,
        int? limit,
        CancellationToken cancellationToken = default);
}
