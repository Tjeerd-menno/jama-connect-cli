using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IItemReader
{
    Task<JamaItem?> GetItemAsync(ItemIdentifier identifier, ItemQueryOptions options, CancellationToken cancellationToken = default);

    Task<JamaPage<JamaItem>> SearchItemsAsync(ItemSearchCriteria criteria, PageRequest page, CancellationToken cancellationToken = default);

    Task<JamaPage<JsonElementBackedVersion>> GetVersionsAsync(ItemIdentifier identifier, PageRequest page, CancellationToken cancellationToken = default);
}
