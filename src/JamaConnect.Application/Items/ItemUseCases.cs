using System.Text.Json;
using JamaConnect.Application.Configuration;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Items;

public sealed class ItemUseCases
{
    private readonly IItemReader _reader;
    private readonly IItemWriter _writer;
    private readonly AliasResolver _aliases;

    public ItemUseCases(IItemReader reader, IItemWriter writer, AliasResolver aliases)
    {
        _reader = reader;
        _writer = writer;
        _aliases = aliases;
    }

    public Task<JamaPage<JamaItem>> SearchAsync(ItemSearchCriteria criteria, PageRequest page, CancellationToken cancellationToken = default)
    {
        return _reader.SearchItemsAsync(criteria, page, cancellationToken);
    }

    public Task<JamaItem?> GetAsync(string identifier, ItemQueryOptions options, CancellationToken cancellationToken = default)
    {
        return _reader.GetItemAsync(ItemIdentifier.Parse(identifier), options, cancellationToken);
    }

    public Task<JsonElement> CreateAsync(int projectId, string type, int? parentId, IReadOnlyDictionary<string, JsonElement> fields, CancellationToken cancellationToken = default)
    {
        return _writer.CreateItemAsync(new CreateItemRequest(projectId, _aliases.ResolveItemTypeId(type), parentId, fields), cancellationToken);
    }

    public Task<JsonElement> PatchAsync(int itemId, IReadOnlyList<JsonPatchOperation> operations, CancellationToken cancellationToken = default)
    {
        return _writer.PatchItemAsync(itemId, operations, cancellationToken);
    }

    public Task DeleteAsync(int itemId, CancellationToken cancellationToken = default)
        => _writer.DeleteItemAsync(itemId, cancellationToken);
}
