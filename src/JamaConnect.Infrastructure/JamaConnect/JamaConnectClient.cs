using System.Net.Http.Headers;
using System.Net.Http.Json;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using JamaConnect.Infrastructure.JamaConnect.Dto;
using JamaConnect.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class JamaConnectClient : IProjectService, IItemService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthenticationService _authenticationService;
    private readonly JamaConnectOptions _options;

    public JamaConnectClient(
        IHttpClientFactory httpClientFactory,
        IAuthenticationService authenticationService,
        IOptions<JamaConnectOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationService = authenticationService;
        _options = options.Value;
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("jama");
        client.BaseAddress = new Uri(_options.BaseUrl);
        var token = await _authenticationService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    public async Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client
            .GetFromJsonAsync<PagedResponse<ProjectDto>>("/rest/v1/projects", cancellationToken)
            .ConfigureAwait(false);
        return response?.Data?.Select(MapProject).ToList().AsReadOnly() ?? (IReadOnlyList<Project>)[];
    }

    public async Task<Project?> GetProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client
            .GetFromJsonAsync<SingleResponse<ProjectDto>>($"/rest/v1/projects/{projectId}", cancellationToken)
            .ConfigureAwait(false);
        return response?.Data is null ? null : MapProject(response.Data);
    }

    public async Task<IReadOnlyList<Item>> GetItemsAsync(int projectId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client
            .GetFromJsonAsync<PagedResponse<ItemDto>>($"/rest/v1/items?project={projectId}", cancellationToken)
            .ConfigureAwait(false);
        return response?.Data?.Select(MapItem).ToList().AsReadOnly() ?? (IReadOnlyList<Item>)[];
    }

    public async Task<Item?> GetItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client
            .GetFromJsonAsync<SingleResponse<ItemDto>>($"/rest/v1/items/{itemId}", cancellationToken)
            .ConfigureAwait(false);
        return response?.Data is null ? null : MapItem(response.Data);
    }

    private static Project MapProject(ProjectDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Fields?.Name ?? string.Empty,
        Description = dto.Fields?.Description,
        IsFolder = dto.IsFolder,
        ProjectKey = dto.ProjectKey,
    };

    private static Item MapItem(ItemDto dto) => new()
    {
        Id = dto.Id,
        DocumentKey = dto.DocumentKey ?? string.Empty,
        Subject = dto.Fields?.Name ?? string.Empty,
        TypeId = dto.ItemType,
        ProjectId = dto.Project,
        ParentId = dto.Parent,
    };
}
