using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<Project?> GetProjectAsync(int projectId, CancellationToken cancellationToken = default);
}
