using System;
using System.Net.Http;

namespace CompanyBrain.Client.Api;

public sealed class ApiSettings
{
    public const string EnvironmentVariable = "COMPANY_BRAIN_API_BASE_URL";
    public const string DefaultBaseUrl = "http://127.0.0.1:8000/api/v1/";

    public Uri BaseAddress { get; }

    public ApiSettings(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var address)
            || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(address.UserInfo)
            || !string.IsNullOrEmpty(address.Query)
            || !string.IsNullOrEmpty(address.Fragment))
        {
            throw new ArgumentException(
                $"{EnvironmentVariable} must be an absolute HTTP(S) URL without credentials, a query, or a fragment.",
                nameof(baseUrl));
        }

        BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/");
    }

    public static ApiSettings FromEnvironment() =>
        new(Environment.GetEnvironmentVariable(EnvironmentVariable) ?? DefaultBaseUrl);

    public HttpClient CreateHttpClient() => new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    })
    {
        BaseAddress = BaseAddress,
        Timeout = TimeSpan.FromSeconds(30),
    };
}
