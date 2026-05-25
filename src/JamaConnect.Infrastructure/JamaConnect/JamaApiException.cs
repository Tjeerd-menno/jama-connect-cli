using System.Net;
using System.Text.Json;

namespace JamaConnect.Infrastructure.JamaConnect;

public sealed class JamaApiException : Exception
{
    public JamaApiException(HttpStatusCode statusCode, string message, JsonElement? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }

    public HttpStatusCode StatusCode { get; }

    public JsonElement? Details { get; }
}
