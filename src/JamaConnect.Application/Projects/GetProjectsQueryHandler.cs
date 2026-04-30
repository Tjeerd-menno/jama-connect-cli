using JamaConnect.Application.Abstractions;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Projects;

public sealed class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, IReadOnlyList<Project>>
{
    private readonly IProjectService _projectService;

    public GetProjectsQueryHandler(IProjectService projectService)
    {
        _projectService = projectService;
    }

    public Task<IReadOnlyList<Project>> HandleAsync(GetProjectsQuery query, CancellationToken cancellationToken = default)
    {
        return _projectService.GetProjectsAsync(cancellationToken);
    }
}
