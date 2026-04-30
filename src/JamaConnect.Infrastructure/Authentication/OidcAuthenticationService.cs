using System.Net.Http.Json;
using System.Text.Json.Serialization;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace JamaConnect.Infrastructure.Authentication;

internal sealed class OidcAuthenticationService : IAuthenticationService, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JamaConnectOptions _options;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public OidcAuthenticationService(IHttpClientFactory httpClientFactory, IOptions<JamaConnectOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsAuthenticated)
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsAuthenticated)
            {
                return _accessToken;
            }

            await LoginAsync(cancellationToken).ConfigureAwait(false);
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient("auth");
        httpClient.BaseAddress = new Uri(_options.BaseUrl);

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
        });

        var response = await httpClient.PostAsync(_options.TokenEndpoint, tokenRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (tokenResponse is not null)
        {
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
        }
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _accessToken = null;
        _tokenExpiry = DateTime.MinValue;
        return Task.CompletedTask;
    }

    public void Dispose() => _tokenLock.Dispose();

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }
    }
}
