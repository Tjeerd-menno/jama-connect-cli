using System.Text.Json;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using JamaConnect.Infrastructure.JamaConnect.Dto;
using JamaConnect.Infrastructure.Json;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class RelationshipAdapter : IRelationshipReader, IRelationshipWriter
{
    private readonly JamaRestClient _client;

    public RelationshipAdapter(JamaRestClient client)
    {
        _client = client;
    }

    public Task<JamaRelationship?> GetRelationshipAsync(int relationshipId, CancellationToken cancellationToken = default)
        => _client.GetSingleAsync($"/rest/v1/relationships/{relationshipId}", Map, cancellationToken);

    public async Task<JamaPage<JamaRelationship>> GetRelationshipsAsync(RelationshipQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        if (query.ItemId is not null && query.Direction.Equals("both", StringComparison.OrdinalIgnoreCase))
        {
            var upstream = await GetRelationshipsAsync(query with { Direction = "upstream" }, page, cancellationToken).ConfigureAwait(false);
            var downstream = await GetRelationshipsAsync(query with { Direction = "downstream" }, page, cancellationToken).ConfigureAwait(false);
            var data = upstream.Data.Concat(downstream.Data).GroupBy(x => x.Id).Select(x => x.First()).ToArray();
            return new JamaPage<JamaRelationship>(page.StartAt, page.MaxResults, data.Length, data.Length, data);
        }

        var path = query.ItemId is null
            ? "/rest/v1/relationships"
            : query.Direction.ToLowerInvariant() switch
            {
                "upstream" => $"/rest/v1/items/{query.ItemId.Value}/upstreamrelationships",
                "downstream" => $"/rest/v1/items/{query.ItemId.Value}/downstreamrelationships",
                _ => $"/rest/v1/items/{query.ItemId.Value}/downstreamrelationships"
            };
        path = new EndpointBuilder(path).AddMany("include", query.Includes).ToString();
        return await _client.GetPageAsync(path, page, Map, cancellationToken).ConfigureAwait(false);
    }

    public Task<JsonElement> CreateRelationshipAsync(CreateRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        var dto = new RequestRelationshipDto(request.FromItemId, request.ToItemId, request.RelationshipTypeId);
        var body = JsonSerializer.SerializeToElement(dto, JamaConnectJsonSerializerContext.Default.RequestRelationshipDto);
        return _client.SendJsonAsync(HttpMethod.Post, "/rest/v1/relationships", body, cancellationToken);
    }

    public Task<JsonElement> UpdateRelationshipAsync(int relationshipId, JsonElement request, CancellationToken cancellationToken = default)
        => _client.SendJsonAsync(HttpMethod.Put, $"/rest/v1/relationships/{relationshipId}", request, cancellationToken);

    public Task DeleteRelationshipAsync(int relationshipId, CancellationToken cancellationToken = default)
        => _client.SendNoContentAsync(HttpMethod.Delete, $"/rest/v1/relationships/{relationshipId}", cancellationToken);

    public Task ClearSuspectAsync(int relationshipId, CancellationToken cancellationToken = default)
        => _client.SendNoContentAsync(HttpMethod.Delete, $"/rest/v1/relationships/{relationshipId}/suspect", cancellationToken);

    private static JamaRelationship Map(JsonElement element)
    {
        return new JamaRelationship(
            element.GetInt("id"),
            element.GetInt("relationshipType"),
            null,
            element.GetInt("fromItem"),
            element.GetInt("toItem"),
            element.GetBool("suspect"),
            element);
    }
}
