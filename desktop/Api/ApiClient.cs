using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CompanyBrain.Client.Api;

public sealed class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
    };

    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (httpClient.BaseAddress is null || !httpClient.BaseAddress.AbsolutePath.EndsWith('/'))
            throw new ArgumentException("An API base address ending in '/' is required.", nameof(httpClient));

        _httpClient = httpClient;
    }

    public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(path, UriKind.Relative, out var relative)
            || path.StartsWith('/') || path.Contains('\\')
            || !new Uri(_httpClient.BaseAddress!, relative).AbsolutePath.StartsWith(
                _httpClient.BaseAddress!.AbsolutePath, StringComparison.Ordinal))
        {
            throw new ArgumentException("Use a path relative to the API base URL, without a leading '/'.", nameof(path));
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relative);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw await CreateHttpErrorAsync(response, cancellationToken).ConfigureAwait(false);

            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                if (result is null)
                    throw new JsonException("The API returned null instead of a JSON response.");

                return result;
            }
            catch (JsonException exception)
            {
                throw new ApiException(ApiErrorKind.InvalidResponse, "The API returned an invalid JSON response.",
                    response.StatusCode, innerException: exception);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException(ApiErrorKind.Timeout, "The API request timed out.", innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new ApiException(ApiErrorKind.Network, "The API could not be reached.", innerException: exception);
        }
        catch (IOException exception)
        {
            throw new ApiException(ApiErrorKind.Network, "The API response could not be received.", innerException: exception);
        }
    }

    private static async Task<ApiException> CreateHttpErrorAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var kind = response.StatusCode switch
        {
            HttpStatusCode.NotFound => ApiErrorKind.NotFound,
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiErrorKind.Validation,
            >= HttpStatusCode.InternalServerError => ApiErrorKind.Server,
            _ => ApiErrorKind.UnexpectedStatus,
        };

        JsonElement? detail = null;
        try
        {
            using var json = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (json?.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("detail", out var value))
            {
                detail = value.Clone();
            }
        }
        catch (JsonException)
        {
        }

        return new ApiException(kind, $"The API returned HTTP {(int)response.StatusCode} ({response.StatusCode}).",
            response.StatusCode, detail);
    }
}
