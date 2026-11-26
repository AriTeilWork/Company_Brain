using CompanyBrain.Client.Api;

namespace CompanyBrain.Client.Tests;

[TestClass]
public sealed class ApiSettingsTests
{
    [TestMethod]
    [DataRow("http://127.0.0.1:8000/api/v1", "http://127.0.0.1:8000/api/v1/")]
    [DataRow("https://gateway.test/company-brain/api/v1/", "https://gateway.test/company-brain/api/v1/")]
    public void PreservesDeploymentPrefixAndNormalizesTrailingSlash(string input, string expected)
    {
        using var http = new ApiSettings(input).CreateHttpClient();

        Assert.AreEqual(expected, http.BaseAddress!.AbsoluteUri);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("gateway.test/api/v1")]
    [DataRow("file:///tmp/api")]
    [DataRow("https://user:secret@gateway.test/api/v1")]
    [DataRow("https://gateway.test/api/v1?q=test")]
    [DataRow("https://gateway.test/api/v1#fragment")]
    public void RejectsInvalidConfiguration(string value)
    {
        Assert.Throws<ArgumentException>(() => new ApiSettings(value));
    }

    [TestMethod]
    [DoNotParallelize]
    public void EnvironmentOverridesDefaultWithoutChangingSourceCode()
    {
        var previous = Environment.GetEnvironmentVariable(ApiSettings.EnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(ApiSettings.EnvironmentVariable, null);
            Assert.AreEqual(ApiSettings.DefaultBaseUrl, ApiSettings.FromEnvironment().BaseAddress.AbsoluteUri);

            Environment.SetEnvironmentVariable(ApiSettings.EnvironmentVariable, "https://gateway.test/api/v1");
            Assert.AreEqual("https://gateway.test/api/v1/", ApiSettings.FromEnvironment().BaseAddress.AbsoluteUri);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ApiSettings.EnvironmentVariable, previous);
        }
    }
}
