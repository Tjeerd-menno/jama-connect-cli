using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class ProjectAdapter : IProjectReader
{
    private readonly JamaRestClient _client;

    public ProjectAdapter(JamaRestClient client)
    {
        _client = client;
    }

    public Task<Project?> GetProjectAsync(int id, CancellationToken cancellationToken = default)
        => _client.GetSingleAsync($"/rest/v1/projects/{id}", Map, cancellationToken);

    public Task<JamaPage<Project>> GetProjectsAsync(PageRequest page, CancellationToken cancellationToken = default)
        => _client.GetPageAsync("/rest/v1/projects", page, Map, cancellationToken);

    private static Project Map(System.Text.Json.JsonElement element)
    {
        var fields = element.GetObject("fields");
        return new Project
        {
            Id = element.GetInt("id"),
            Name = fields?.GetStringOrNull("name") ?? string.Empty,
            Description = fields?.GetStringOrNull("description"),
            IsFolder = element.GetBool("isFolder"),
            ProjectKey = element.GetStringOrNull("projectKey")
        };
    }
}
