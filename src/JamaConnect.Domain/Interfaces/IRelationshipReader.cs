using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IRelationshipReader
{
    Task<JamaRelationship?> GetRelationshipAsync(int relationshipId, CancellationToken cancellationToken = default);

    Task<JamaPage<JamaRelationship>> GetRelationshipsAsync(RelationshipQuery query, PageRequest page, CancellationToken cancellationToken = default);
}
