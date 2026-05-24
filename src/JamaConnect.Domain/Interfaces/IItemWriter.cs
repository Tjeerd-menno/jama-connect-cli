using System.Text.Json;
using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IItemWriter
{
    Task<JsonElement> CreateItemAsync(CreateItemRequest request, CancellationToken cancellationToken = default);

    Task<JsonElement> UpdateItemAsync(int itemId, JsonElement request, CancellationToken cancellationToken = default);

    Task<JsonElement> PatchItemAsync(int itemId, IReadOnlyList<JsonPatchOperation> operations, CancellationToken cancellationToken = default);

    Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default);
}
