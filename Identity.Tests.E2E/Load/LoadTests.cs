namespace Identity.Tests.E2E.Load;

using Identity.Tests.E2E.Infrastructure;
using Identity.Tests.E2E.Oidc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[Trait("Category", "Load")]
[Collection(E2ECollection.Name)]
public sealed class LoadTests : IDisposable
{
    private const string LoginPath = PageRoutes.Login;
    private const string HealthPath = PageRoutes.Health;

    private readonly HttpClient _httpClient;
    private readonly LoadSettings _settings;

    public LoadTests(PlaywrightFixture fixture)
    {
        _settings = LoadSettings.Read(fixture.Factory.Services.GetRequiredService<IConfiguration>());
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
    public async Task DiscoveryEndpoint_UnderConcurrentLoad_HasNegligibleFailures() =>
        await AssertFailRateWithinCeilingAsync(
            Profile(OidcDiscoveryConstants.DiscoveryPath, _settings.DiscoveryRequests, _settings.DiscoveryParallelism),
            _settings.MaxFailRate);

    [Fact]
    public async Task LoginPage_UnderConcurrentLoad_HasNegligibleFailures() =>
        await AssertFailRateWithinCeilingAsync(
            Profile(LoginPath, _settings.LoginRequests, _settings.LoginParallelism),
            _settings.MaxLoginFailRate);

    [Fact]
    public async Task JwksEndpoint_UnderConcurrentLoad_HasNegligibleFailures() =>
        await AssertFailRateWithinCeilingAsync(
            Profile(OidcDiscoveryConstants.JwksPath, _settings.JwksRequests, _settings.JwksParallelism),
            _settings.MaxFailRate);

    [Fact]
    public async Task HealthEndpoint_UnderConcurrentLoad_AllSucceed()
    {
        var profile = Profile(HealthPath, _settings.HealthRequests, _settings.HealthParallelism);
        var failed = await RunLoadAsync(profile);

        Assert.Equal(0, failed);
    }

    public void Dispose() => _httpClient.Dispose();

    private static double FailRate(int requests, int failed) =>
        requests > 0 ? (double)failed / requests : 0.0;

    private LoadProfile Profile(string path, int requests, int parallelism) =>
        new(path, Math.Min(requests * _settings.RequestScale, _settings.MaxRequests), parallelism);

    private async Task AssertFailRateWithinCeilingAsync(LoadProfile profile, double ceiling)
    {
        var failed = await RunLoadAsync(profile);
        var failRate = FailRate(profile.Requests, failed);

        Assert.True(
            failRate < ceiling,
            $"{profile.Path} failed {failed} of {profile.Requests} requests ({failRate:P1}), above the {ceiling:P1} ceiling.");
    }

    private async Task<int> RunLoadAsync(LoadProfile profile)
    {
        var failed = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, profile.Requests),
            new ParallelOptions { MaxDegreeOfParallelism = profile.Parallelism },
            async (_, ct) =>
            {
                try
                {
                    using var response = await _httpClient.GetAsync(profile.Path, ct);
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

        return failed;
    }
}
