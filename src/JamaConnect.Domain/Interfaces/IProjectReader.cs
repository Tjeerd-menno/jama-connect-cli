using JamaConnect.Domain.Models;

namespace JamaConnect.Domain.Interfaces;

public interface IProjectReader
{
    Task<Project?> GetProjectAsync(int id, CancellationToken cancellationToken = default);

    Task<JamaPage<Project>> GetProjectsAsync(PageRequest page, CancellationToken cancellationToken = default);
}
