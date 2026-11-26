using System;
using System.Net;
using System.Text.Json;

namespace CompanyBrain.Client.Api;

public enum ApiErrorKind
{
    NotFound,
    Validation,
    Network,
    Timeout,
    Server,
    UnexpectedStatus,
    InvalidResponse,
}

public sealed class ApiException : Exception
{
    public ApiErrorKind Kind { get; }
    public HttpStatusCode? StatusCode { get; }
    public JsonElement? Detail { get; }

    public ApiException(ApiErrorKind kind, string message, HttpStatusCode? statusCode = null,
        JsonElement? detail = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        StatusCode = statusCode;
        Detail = detail;
    }
}
