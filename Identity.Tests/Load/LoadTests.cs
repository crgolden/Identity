namespace Identity.Tests.Load;

using Infrastructure;
using NBomber.CSharp;
using NBomber.Http.CSharp;

/// <summary>
/// Load tests that measure throughput and failure rate of core endpoints.
/// Run separately from unit/E2E tests: --filter-trait Category=Load
/// </summary>
[Trait("Category", "Load")]
[Collection(E2ECollection.Name)]
public sealed class LoadTests(PlaywrightFixture fixture) : IDisposable
{
    // Ignore the self-signed test certificate from the Kestrel server.
    private readonly HttpClient _httpClient = new(
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
    {
        BaseAddress = new Uri(fixture.BaseAddress)
    };

    [Fact]
    public void DiscoveryEndpoint_Under50Rps_HasNegligibleFailures()
    {
        var scenario = Scenario.Create("discovery_endpoint", async context =>
        {
            var request = Http.CreateRequest("GET", "/.well-known/openid-configuration");
            return await Http.Send(_httpClient, request, context.CancellationToken);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 50,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(15)));

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var scenarioStats = stats.ScenarioStats.First(s => s.ScenarioName == "discovery_endpoint");
        var failPercent = scenarioStats.AllRequestCount > 0
            ? (double)scenarioStats.Fail.Request.Count / scenarioStats.AllRequestCount * 100.0
            : 0.0;
        Assert.True(failPercent < 1.0, $"Discovery endpoint fail rate {failPercent:F1}% exceeds 1% threshold.");
    }

    [Fact]
    public void LoginPage_Under30Rps_HasNegligibleFailures()
    {
        var scenario = Scenario.Create("login_page_get", async context =>
        {
            var request = Http.CreateRequest("GET", "/Account/Login");
            return await Http.Send(_httpClient, request, context.CancellationToken);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 30,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10)));

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var scenarioStats = stats.ScenarioStats.First(s => s.ScenarioName == "login_page_get");
        var failPercent = scenarioStats.AllRequestCount > 0
            ? (double)scenarioStats.Fail.Request.Count / scenarioStats.AllRequestCount * 100.0
            : 0.0;
        Assert.True(failPercent < 2.0, $"Login page GET fail rate {failPercent:F1}% exceeds 2% threshold.");
    }

    [Fact]
    public void JwksEndpoint_Under100Rps_HasNegligibleFailures()
    {
        var scenario = Scenario.Create("jwks_endpoint", async context =>
        {
            var request = Http.CreateRequest("GET", "/.well-known/openid-configuration/jwks");
            return await Http.Send(_httpClient, request, context.CancellationToken);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10)));

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var scenarioStats = stats.ScenarioStats.First(s => s.ScenarioName == "jwks_endpoint");
        var failPercent = scenarioStats.AllRequestCount > 0
            ? (double)scenarioStats.Fail.Request.Count / scenarioStats.AllRequestCount * 100.0
            : 0.0;
        Assert.True(failPercent < 1.0, $"JWKS endpoint fail rate {failPercent:F1}% exceeds 1% threshold.");
    }

    [Fact]
    public void HealthEndpoint_Under20Rps_AllSucceed()
    {
        var scenario = Scenario.Create("health_endpoint", async context =>
        {
            var request = Http.CreateRequest("GET", "/Health");
            return await Http.Send(_httpClient, request, context.CancellationToken);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 20,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10)));

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var scenarioStats = stats.ScenarioStats.First(s => s.ScenarioName == "health_endpoint");
        Assert.Equal(0, scenarioStats.Fail.Request.Count);
    }

    public void Dispose() => _httpClient.Dispose();
}
