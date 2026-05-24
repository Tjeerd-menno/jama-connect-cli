using System.Text.Json;
using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IRelationshipWriter
{
    Task<JsonElement> CreateRelationshipAsync(CreateRelationshipRequest request, CancellationToken cancellationToken = default);

    Task<JsonElement> UpdateRelationshipAsync(int relationshipId, JsonElement request, CancellationToken cancellationToken = default);

    Task DeleteRelationshipAsync(int relationshipId, CancellationToken cancellationToken = default);

    Task ClearSuspectAsync(int relationshipId, CancellationToken cancellationToken = default);
}
