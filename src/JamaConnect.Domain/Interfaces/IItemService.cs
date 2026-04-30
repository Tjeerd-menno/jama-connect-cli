using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IItemService
{
    Task<IReadOnlyList<Item>> GetItemsAsync(int projectId, CancellationToken cancellationToken = default);
    Task<Item?> GetItemAsync(int itemId, CancellationToken cancellationToken = default);
}
