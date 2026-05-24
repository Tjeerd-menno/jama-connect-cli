using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface ISchemaReader
{
    Task<JamaPage<ItemTypeDefinition>> GetItemTypesAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<ItemTypeDefinition?> GetItemTypeAsync(int id, CancellationToken cancellationToken = default);

    Task<JamaPage<RelationshipTypeDefinition>> GetRelationshipTypesAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<JamaPage<JsonElementBackedVersion>> GetPickListsAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<JsonElementBackedVersion?> GetPickListAsync(int id, CancellationToken cancellationToken = default);
}
