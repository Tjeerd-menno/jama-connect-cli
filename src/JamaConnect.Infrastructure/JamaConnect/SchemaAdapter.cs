using System.Text.Json;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class SchemaAdapter : ISchemaReader
{
    private readonly JamaRestClient _client;

    public SchemaAdapter(JamaRestClient client)
    {
        _client = client;
    }

    public Task<JamaPage<ItemTypeDefinition>> GetItemTypesAsync(PageRequest page, CancellationToken cancellationToken = default)
        => _client.GetPageAsync("/rest/v1/itemtypes", page, MapItemType, cancellationToken);

    public Task<ItemTypeDefinition?> GetItemTypeAsync(int id, CancellationToken cancellationToken = default)
        => _client.GetSingleAsync($"/rest/v1/itemtypes/{id}", MapItemType, cancellationToken);

    public Task<JamaPage<RelationshipTypeDefinition>> GetRelationshipTypesAsync(PageRequest page, CancellationToken cancellationToken = default)
        => _client.GetPageAsync("/rest/v1/relationshiptypes", page, x => new RelationshipTypeDefinition(x.GetInt("id"), x.GetStringOrNull("name") ?? string.Empty, x), cancellationToken);

    public Task<RelationshipTypeDefinition?> GetRelationshipTypeAsync(int id, CancellationToken cancellationToken = default)
        => _client.GetSingleAsync($"/rest/v1/relationshiptypes/{id}", x => new RelationshipTypeDefinition(x.GetInt("id"), x.GetStringOrNull("name") ?? string.Empty, x), cancellationToken);

    public Task<JamaPage<JsonElementBackedVersion>> GetPickListsAsync(PageRequest page, CancellationToken cancellationToken = default)
        => _client.GetPageAsync("/rest/v1/picklists", page, x => new JsonElementBackedVersion(x.GetInt("id"), x), cancellationToken);

    public Task<JsonElementBackedVersion?> GetPickListAsync(int id, CancellationToken cancellationToken = default)
        => _client.GetSingleAsync($"/rest/v1/picklists/{id}", x => new JsonElementBackedVersion(x.GetInt("id"), x), cancellationToken);

    private static ItemTypeDefinition MapItemType(JsonElement element)
    {
        var fields = new List<FieldDefinition>();
        if (element.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in fieldsElement.EnumerateArray())
            {
                fields.Add(new FieldDefinition(
                    field.GetStringOrNull("name") ?? string.Empty,
                    field.GetStringOrNull("label"),
                    field.GetStringOrNull("fieldType") ?? field.GetStringOrNull("type"),
                    field.TryGetProperty("required", out var required) && required.ValueKind is JsonValueKind.True or JsonValueKind.False ? required.GetBoolean() : null,
                    field.GetNullableInt("pickList"),
                    field));
            }
        }

        return new ItemTypeDefinition(element.GetInt("id"), element.GetStringOrNull("name") ?? string.Empty, fields, element);
    }
}
