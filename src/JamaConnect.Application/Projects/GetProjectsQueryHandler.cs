using JamaConnect.Application.Abstractions;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Application.Projects;

public sealed class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, IReadOnlyList<Project>>
{
    private readonly IProjectReader _projectReader;

    public GetProjectsQueryHandler(IProjectReader projectReader)
    {
        _projectReader = projectReader;
    }

    public async Task<IReadOnlyList<Project>> HandleAsync(GetProjectsQuery query, CancellationToken cancellationToken = default)
    {
        var page = await _projectReader.GetProjectsAsync(new PageRequest(), cancellationToken).ConfigureAwait(false);
        return page.Data;
    }
}
