using System.Net.Http.Headers;
using System.Net.Http.Json;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using JamaConnect.Infrastructure.JamaConnect.Dto;
using JamaConnect.Infrastructure.Json;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class JamaConnectClient : IProjectService, IItemService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthenticationService _authenticationService;

    public JamaConnectClient(
        IHttpClientFactory httpClientFactory,
        IAuthenticationService authenticationService)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationService = authenticationService;
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("jama");
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
            .GetFromJsonAsync(
                "/rest/v1/projects",
                JamaConnectJsonSerializerContext.Default.PagedProjectResponse,
                cancellationToken)
            .ConfigureAwait(false);
        return response?.Data?.Select(MapProject).ToList().AsReadOnly() ?? (IReadOnlyList<Project>)[];
    }

    public async Task<Project?> GetProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client
            .GetFromJsonAsync(
                $"/rest/v1/projects/{projectId}",
                JamaConnectJsonSerializerContext.Default.SingleProjectResponse,
                cancellationToken)
            .ConfigureAwait(false);
        return response?.Data is null ? null : MapProject(response.Data);
    }

    public async Task<IReadOnlyList<Item>> GetItemsAsync(int projectId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client
            .GetFromJsonAsync(
                $"/rest/v1/items?project={projectId}",
                JamaConnectJsonSerializerContext.Default.PagedItemResponse,
                cancellationToken)
            .ConfigureAwait(false);
        return response?.Data?.Select(MapItem).ToList().AsReadOnly() ?? (IReadOnlyList<Item>)[];
    }

    public async Task<Item?> GetItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        using var client = await CreateAuthorizedClientAsync(cancellationToken).ConfigureAwait(false);
        var response = await client
            .GetFromJsonAsync(
                $"/rest/v1/items/{itemId}",
                JamaConnectJsonSerializerContext.Default.SingleItemResponse,
                cancellationToken)
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
