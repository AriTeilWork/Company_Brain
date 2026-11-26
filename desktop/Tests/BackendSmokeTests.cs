using CompanyBrain.Client.Api;

namespace CompanyBrain.Client.Tests;

[TestClass]
public sealed class BackendSmokeTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task ReadsExistingBackendRootThroughSharedClient()
    {
        var baseUrl = Environment.GetEnvironmentVariable("COMPANY_BRAIN_SMOKE_BASE_URL");
        if (string.IsNullOrEmpty(baseUrl))
            Assert.Inconclusive("Set COMPANY_BRAIN_SMOKE_BASE_URL to the running backend root URL to run this check.");

        using var http = new ApiSettings(baseUrl).CreateHttpClient();
        var response = await new ApiClient(http).GetAsync<RootResponse>("");

        Assert.AreEqual("Company Brain API", response.Message);
    }

    public sealed record RootResponse(string Message);
}
