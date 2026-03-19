namespace Identity.Tests.Load;

using Infrastructure;

/// <summary>
/// Load tests that measure throughput and failure rate of core endpoints
/// using parallel HttpClient requests. Run separately from unit/E2E tests:
/// --filter-trait Category=Load
/// </summary>
[Trait("Category", "Load")]
[Collection(E2ECollection.Name)]
public sealed class LoadTests : IDisposable
{
    // Ignore the self-signed test certificate from the Kestrel server.
    private readonly HttpClient _httpClient;

    public LoadTests(PlaywrightFixture fixture)
    {
        _httpClient = new HttpClient(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            })
        {
            BaseAddress = new Uri(fixture.BaseAddress)
        };
    }

    [Fact]
    public async Task DiscoveryEndpoint_Under50Rps_HasNegligibleFailures()
    {
        var (total, failed) = await RunLoadAsync(
            "/.well-known/openid-configuration",
            requestCount: 100,
            parallelism: 10);

        var failPercent = total > 0 ? (double)failed / total * 100.0 : 0.0;
        Assert.True(failPercent < 1.0, $"Discovery endpoint fail rate {failPercent:F1}% exceeds 1% threshold.");
    }

    [Fact]
    public async Task LoginPage_Under30Rps_HasNegligibleFailures()
    {
        var (total, failed) = await RunLoadAsync(
            "/Account/Login",
            requestCount: 60,
            parallelism: 10);

        var failPercent = total > 0 ? (double)failed / total * 100.0 : 0.0;
        Assert.True(failPercent < 2.0, $"Login page GET fail rate {failPercent:F1}% exceeds 2% threshold.");
    }

    [Fact]
    public async Task JwksEndpoint_Under100Rps_HasNegligibleFailures()
    {
        var (total, failed) = await RunLoadAsync(
            "/.well-known/openid-configuration/jwks",
            requestCount: 100,
            parallelism: 20);

        var failPercent = total > 0 ? (double)failed / total * 100.0 : 0.0;
        Assert.True(failPercent < 1.0, $"JWKS endpoint fail rate {failPercent:F1}% exceeds 1% threshold.");
    }

    [Fact]
    public async Task HealthEndpoint_Under20Rps_AllSucceed()
    {
        var (total, failed) = await RunLoadAsync(
            "/Health",
            requestCount: 40,
            parallelism: 5);

        Assert.Equal(0, failed);
        Assert.Equal(40, total);
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<(int Total, int Failed)> RunLoadAsync(
        string path,
        int requestCount,
        int parallelism)
    {
        var total = 0;
        var failed = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, requestCount),
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            async (_, ct) =>
            {
                Interlocked.Increment(ref total);
                try
                {
                    var response = await _httpClient.GetAsync(path, ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        Interlocked.Increment(ref failed);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref failed);
                }
            });

        return (total, failed);
    }
}
