using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using JamaConnect.Infrastructure.JamaConnect.Dto;
using JamaConnect.Infrastructure.Json;
using JamaConnect.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class JamaRestClient
{
    private static readonly HashSet<HttpStatusCode> DefaultRetryStatusCodes =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthenticationService _authenticationService;
    private readonly JamaConnectOptions _options;

    public JamaRestClient(
        IHttpClientFactory httpClientFactory,
        IAuthenticationService authenticationService,
        IOptions<JamaConnectOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationService = authenticationService;
        _options = options.Value;
    }

    public async Task<JamaPage<T>> GetPageAsync<T>(
        string path,
        PageRequest page,
        Func<JsonElement, T> map,
        CancellationToken cancellationToken)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var requestPath = $"{path}{separator}startAt={page.StartAt}&maxResults={Math.Min(page.MaxResults, 50)}";
        var response = await SendAsync(HttpMethod.Get, requestPath, null, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content
            .ReadFromJsonAsync(JamaConnectJsonSerializerContext.Default.JsonPagedResponse, cancellationToken)
            .ConfigureAwait(false);
        var data = (payload?.Data ?? []).Select(x => map(Clone(x))).ToArray();
        var pageInfo = payload?.Meta?.PageInfo;
        return new JamaPage<T>(
            pageInfo?.StartIndex ?? page.StartAt,
            Math.Min(page.MaxResults, 50),
            pageInfo?.ResultCount ?? data.Length,
            pageInfo?.TotalResults ?? data.Length,
            data);
    }

    public async Task<T?> GetSingleAsync<T>(string path, Func<JsonElement, T> map, CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content
            .ReadFromJsonAsync(JamaConnectJsonSerializerContext.Default.JsonSingleResponse, cancellationToken)
            .ConfigureAwait(false);
        return payload is null || payload.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? default
            : map(Clone(payload.Data));
    }

    public async Task<JsonElement> SendJsonAsync(HttpMethod method, string path, JsonElement body, CancellationToken cancellationToken)
    {
        using var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        var response = await SendAsync(method, path, content, cancellationToken).ConfigureAwait(false);
        return await ReadDataOrEmptyAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JsonElement> SendPatchAsync(string path, IReadOnlyList<JsonPatchOperation> operations, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(operations.Select(x => new PatchDto(x.Op, x.Path, x.Value)).ToArray(), JamaConnectJsonSerializerContext.Default.PatchDtoArray);
        using var content = new StringContent(body, Encoding.UTF8, "application/json-patch+json");
        var response = await SendAsync(HttpMethod.Patch, path, content, cancellationToken).ConfigureAwait(false);
        return await ReadDataOrEmptyAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendNoContentAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        _ = await SendAsync(method, path, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("xJamaDateFieldsWithTime", "true");
            var token = await _authenticationService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (content is not null)
            {
                request.Content = content;
            }

            var client = _httpClientFactory.CreateClient("jama");
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (attempt < _options.RetryMaxAttempts && DefaultRetryStatusCodes.Contains(response.StatusCode))
            {
                response.Dispose();
                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var details = await TryReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            var message = $"Jama API returned {(int)response.StatusCode} {response.ReasonPhrase}.";
            response.Dispose();
            throw new JamaApiException(response.StatusCode, message, details);
        }
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var initial = Math.Max(1, _options.RetryInitialDelayMilliseconds);
        var max = Math.Max(1, _options.RetryMaxDelaySeconds);
        var milliseconds = Math.Min(initial * Math.Pow(2, attempt - 1), max * 1000);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static async Task<JsonElement> ReadDataOrEmptyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return JsonElement.Parse("{}");
        }

        var payload = await response.Content
            .ReadFromJsonAsync(JamaConnectJsonSerializerContext.Default.JsonSingleResponse, cancellationToken)
            .ConfigureAwait(false);
        return payload?.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? JsonElement.Parse("{}")
            : Clone(payload!.Data);
    }

    private static async Task<JsonElement?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content
                .ReadFromJsonAsync(JamaConnectJsonSerializerContext.Default.JsonErrorResponse, cancellationToken)
                .ConfigureAwait(false);
            return error is null ? null : JsonSerializer.SerializeToElement(error, JamaConnectJsonSerializerContext.Default.JsonErrorResponse);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement Clone(JsonElement element) => element.Clone();

}
