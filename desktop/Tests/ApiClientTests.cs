using System.Net;
using System.Text;
using CompanyBrain.Client.Api;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace CompanyBrain.Client.Tests;

[TestClass]
public sealed class ApiClientTests
{
    [TestMethod]
    public void RejectsMissingHttpClient()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiClient(null!));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("https://gateway.test/api/v1")]
    public void RejectsBaseAddressesThatCannotPreserveTheApiPrefix(string? baseUrl)
    {
        using var http = new HttpClient { BaseAddress = baseUrl is null ? null : new Uri(baseUrl) };

        Assert.Throws<ArgumentException>(() => new ApiClient(http));
    }

    [TestMethod]
    public async Task GetPreservesBasePathAndQueryAndReadsSnakeCaseJson()
    {
        using var http = CreateHttp((request, _) =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https://gateway.test/company-brain/api/v1/customers?q=West%20Quay&limit=20&offset=0",
                request.RequestUri!.AbsoluteUri);
            Assert.AreEqual("application/json", request.Headers.Accept.Single().MediaType);
            return Task.FromResult(Json(HttpStatusCode.OK,
                """{"customer_id":"CUST-002","organisation_name":"West Quay Accounting"}"""));
        });

        var result = await new ApiClient(http).GetAsync<Customer>("customers?q=West%20Quay&limit=20&offset=0");

        Assert.AreEqual("CUST-002", result.CustomerId);
        Assert.AreEqual("West Quay Accounting", result.OrganisationName);
    }

    [TestMethod]
    [DataRow("""{"items":[],"total":0,"limit":20,"offset":0}""", 0)]
    [DataRow("""{"items":[{"customer_id":"CUST-002","organisation_name":"West Quay"}],"total":21,"limit":20,"offset":20}""", 1)]
    public async Task ReadsBothEmptyAndPopulatedListEnvelopes(string json, int count)
    {
        using var http = CreateHttp((_, _) => Task.FromResult(Json(HttpStatusCode.OK, json)));

        var page = await new ApiClient(http).GetAsync<ListResponse<Customer>>("customers");

        Assert.AreEqual(count, page.Items.Count);
        Assert.AreEqual(count == 0 ? 0 : 21, page.Total);
        Assert.AreEqual(20, page.Limit);
        Assert.AreEqual(count == 0 ? 0 : 20, page.Offset);
    }

    [TestMethod]
    [DataRow(404, ApiErrorKind.NotFound)]
    [DataRow(400, ApiErrorKind.Validation)]
    [DataRow(422, ApiErrorKind.Validation)]
    [DataRow(500, ApiErrorKind.Server)]
    [DataRow(503, ApiErrorKind.Server)]
    [DataRow(401, ApiErrorKind.UnexpectedStatus)]
    public async Task KeepsHttpErrorsDistinctFromSuccessfulResponses(int status, ApiErrorKind kind)
    {
        using var http = CreateHttp((_, _) => Task.FromResult(Json((HttpStatusCode)status,
            """{"detail":"Example API error"}""")));

        var error = await Assert.ThrowsAsync<ApiException>(() =>
            new ApiClient(http).GetAsync<ListResponse<Customer>>("customers/unknown"));

        Assert.AreEqual(kind, error.Kind);
        Assert.AreEqual((HttpStatusCode)status, error.StatusCode);
        Assert.AreEqual("Example API error", error.Detail!.Value.GetString());
    }

    [TestMethod]
    public async Task PreservesFastApiValidationDetailsAfterResponseIsDisposed()
    {
        using var http = CreateHttp((_, _) => Task.FromResult(Json(HttpStatusCode.UnprocessableEntity,
            """{"detail":[{"loc":["query","limit"],"msg":"Must be at most 100","type":"less_than_equal"}]}""")));

        var error = await Assert.ThrowsAsync<ApiException>(() => new ApiClient(http).GetAsync<Customer>("customers?limit=101"));

        Assert.AreEqual(ApiErrorKind.Validation, error.Kind);
        Assert.AreEqual("limit", error.Detail!.Value[0].GetProperty("loc")[1].GetString());
    }

    [TestMethod]
    [DataRow("<html>Bad gateway</html>")]
    [DataRow("")]
    public async Task NonJsonGatewayErrorsKeepTheirHttpStatus(string body)
    {
        using var http = CreateHttp((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/html"),
        }));

        var error = await Assert.ThrowsAsync<ApiException>(() => new ApiClient(http).GetAsync<Customer>("customers"));

        Assert.AreEqual(ApiErrorKind.Server, error.Kind);
        Assert.AreEqual(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.IsNull(error.Detail);
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("[]")]
    [DataRow("{}")]
    [DataRow("""{"message":"Temporarily unavailable"}""")]
    public async Task ErrorPayloadWithoutDetailStillReportsServerFailure(string body)
    {
        using var http = CreateHttp((_, _) => Task.FromResult(Json(HttpStatusCode.ServiceUnavailable, body)));

        var error = await Assert.ThrowsAsync<ApiException>(() => new ApiClient(http).GetAsync<Customer>("customers"));

        Assert.AreEqual(ApiErrorKind.Server, error.Kind);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.IsNull(error.Detail);
    }

    [TestMethod]
    [DataRow("not JSON")]
    [DataRow("null")]
    [DataRow("")]
    [DataRow("{}")]
    [DataRow("""{"items":null,"total":0,"limit":20,"offset":0}""")]
    public async Task InvalidOrIncompleteSuccessPayloadIsReported(string body)
    {
        using var http = CreateHttp((_, _) => Task.FromResult(Json(HttpStatusCode.OK, body)));

        var error = await Assert.ThrowsAsync<ApiException>(() =>
            new ApiClient(http).GetAsync<ListResponse<Customer>>("customers"));

        Assert.AreEqual(ApiErrorKind.InvalidResponse, error.Kind);
        Assert.AreEqual(HttpStatusCode.OK, error.StatusCode);
    }

    [TestMethod]
    public async Task PendingRequestsCompleteAsynchronously()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = CreateHttp((_, _) => response.Task);

        var pending = new ApiClient(http).GetAsync<Customer>("customers/CUST-002");
        Assert.IsFalse(pending.IsCompleted);
        response.SetResult(Json(HttpStatusCode.OK, """{"customer_id":"CUST-002","organisation_name":"West Quay"}"""));

        Assert.AreEqual("CUST-002", (await pending.WaitAsync(TimeSpan.FromSeconds(5))).CustomerId);
    }

    [TestMethod]
    public async Task CallerCancellationRemainsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = CreateHttp(async (_, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return Json(HttpStatusCode.OK, "{}");
        });

        var pending = new ApiClient(http).GetAsync<Customer>("customers", cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => pending);
    }

    [TestMethod]
    public async Task AlreadyCancelledRequestPropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var http = CreateHttp((_, token) => Task.FromCanceled<HttpResponseMessage>(token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ApiClient(http).GetAsync<Customer>("customers", cancellation.Token));
    }

    [TestMethod]
    public async Task TimeoutIsDifferentFromCallerCancellation()
    {
        using var http = CreateHttp(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return Json(HttpStatusCode.OK, "{}");
        });
        http.Timeout = TimeSpan.FromMilliseconds(100);

        var error = await Assert.ThrowsAsync<ApiException>(() => new ApiClient(http).GetAsync<Customer>("customers"));

        Assert.AreEqual(ApiErrorKind.Timeout, error.Kind);
        Assert.IsNull(error.StatusCode);
    }

    [TestMethod]
    public async Task ConnectionFailureIsReportedAsNetworkError()
    {
        using var http = CreateHttp((_, _) => throw new HttpRequestException("Connection refused"));

        var error = await Assert.ThrowsAsync<ApiException>(() => new ApiClient(http).GetAsync<Customer>("customers"));

        Assert.AreEqual(ApiErrorKind.Network, error.Kind);
        Assert.IsNull(error.StatusCode);
    }

    [TestMethod]
    public async Task TransportReadFailurePreservesItsCause()
    {
        var failure = new IOException("Connection interrupted while receiving the response.");
        using var http = CreateHttp((_, _) => throw failure);

        var error = await Assert.ThrowsAsync<ApiException>(() => new ApiClient(http).GetAsync<Customer>("customers"));

        Assert.AreEqual(ApiErrorKind.Network, error.Kind);
        Assert.IsNull(error.StatusCode);
        Assert.AreSame(failure, error.InnerException);
    }

    [TestMethod]
    public async Task EmptyPathRequestsTheConfiguredApiRoot()
    {
        using var http = CreateHttp((request, _) =>
        {
            Assert.AreEqual("https://gateway.test/company-brain/api/v1/", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(Json(HttpStatusCode.OK,
                """{"customer_id":"CUST-002","organisation_name":"West Quay"}"""));
        });

        Assert.AreEqual("CUST-002", (await new ApiClient(http).GetAsync<Customer>("")).CustomerId);
    }

    [TestMethod]
    [DataRow("https://other.test/customers")]
    [DataRow("/customers")]
    [DataRow("../customers")]
    [DataRow("..\\customers")]
    public async Task InvalidPathsCannotDiscardConfiguredBasePath(string path)
    {
        using var http = CreateHttp((_, _) => throw new AssertFailedException("No request should be sent."));

        await Assert.ThrowsAsync<ArgumentException>(() => new ApiClient(http).GetAsync<Customer>(path));
    }

    private static HttpClient CreateHttp(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) =>
        new(new StubHandler(send)) { BaseAddress = new Uri("https://gateway.test/company-brain/api/v1/") };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }

    public sealed record Customer(string CustomerId, string OrganisationName);
}
