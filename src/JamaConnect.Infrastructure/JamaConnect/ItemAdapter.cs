using System.Text.Json;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using JamaConnect.Infrastructure.JamaConnect.Dto;
using JamaConnect.Infrastructure.Json;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class ItemAdapter : IItemReader, IItemWriter
{
    private readonly JamaRestClient _client;

    public ItemAdapter(JamaRestClient client)
    {
        _client = client;
    }

    public async Task<JamaItem?> GetItemAsync(ItemIdentifier identifier, ItemQueryOptions options, CancellationToken cancellationToken = default)
    {
        if (identifier.Id is not null)
        {
            var path = new EndpointBuilder($"/rest/v1/items/{identifier.Id.Value}")
                .AddMany("include", options.Includes)
                .ToString();
            return await _client.GetSingleAsync(path, Map, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(identifier.DocumentKey))
        {
            var page = await SearchItemsAsync(new ItemSearchCriteria(null, null, identifier.DocumentKey, null, null, null, false, options.Includes), new PageRequest(0, 1), cancellationToken).ConfigureAwait(false);
            return page.Data.Count > 0 ? page.Data[0] : null;
        }

        return null;
    }

    public Task<JamaPage<JamaItem>> SearchItemsAsync(ItemSearchCriteria criteria, PageRequest page, CancellationToken cancellationToken = default)
    {
        var path = new EndpointBuilder("/rest/v1/abstractitems")
            .Add("project", criteria.ProjectId)
            .Add("itemType", criteria.Type)
            .Add("documentKey", criteria.DocumentKey)
            .Add("contains", criteria.Contains)
            .Add("createdDate", criteria.CreatedSince)
            .Add("modifiedDate", criteria.ModifiedSince)
            .Add("rootOnly", criteria.RootOnly ? "true" : null)
            .AddMany("include", criteria.Includes)
            .ToString();
        return _client.GetPageAsync(path, page, Map, cancellationToken);
    }

    public Task<JamaPage<JsonElementBackedVersion>> GetVersionsAsync(ItemIdentifier identifier, PageRequest page, CancellationToken cancellationToken = default)
    {
        if (identifier.Id is null)
        {
            throw new InvalidOperationException("Item versions require a numeric Jama item id.");
        }

        return _client.GetPageAsync($"/rest/v1/items/{identifier.Id.Value}/versions", page, x => new JsonElementBackedVersion(x.GetInt("id"), x), cancellationToken);
    }

    public Task<JsonElement> CreateItemAsync(CreateItemRequest request, CancellationToken cancellationToken = default)
    {
        var dto = new RequestItemDto(
            request.ProjectId,
            request.ItemTypeId,
            request.ParentId is null ? null : new RequestItemLocationDto(request.ParentId.Value),
            request.GlobalId,
            request.Fields);
        var body = JsonSerializer.SerializeToElement(dto, JamaConnectJsonSerializerContext.Default.RequestItemDto);
        return _client.SendJsonAsync(HttpMethod.Post, "/rest/v1/items", body, cancellationToken);
    }

    public Task<JsonElement> UpdateItemAsync(int itemId, JsonElement request, CancellationToken cancellationToken = default)
        => _client.SendJsonAsync(HttpMethod.Put, $"/rest/v1/items/{itemId}", request, cancellationToken);

    public Task<JsonElement> PatchItemAsync(int itemId, IReadOnlyList<JsonPatchOperation> operations, CancellationToken cancellationToken = default)
        => _client.SendPatchAsync($"/rest/v1/items/{itemId}", operations, cancellationToken);

    public Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
        => _client.SendNoContentAsync(HttpMethod.Delete, $"/rest/v1/items/{itemId}", cancellationToken);

    private static JamaItem Map(JsonElement element)
    {
        var fields = element.GetObject("fields");
        return new JamaItem(
            element.GetInt("id"),
            element.GetStringOrNull("documentKey"),
            element.GetStringOrNull("globalId"),
            element.GetInt("project"),
            element.GetInt("itemType"),
            null,
            element.GetNullableInt("parent"),
            fields?.GetStringOrNull("name") ?? element.GetStringOrNull("name") ?? string.Empty,
            fields.ToDictionary(),
            element.Clone());
    }
}
