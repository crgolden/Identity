namespace Identity.Tests.E2E.Synthetic;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

public sealed class IdentityWalkerFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private PlaywrightSettings? _playwrightSettings;
    private WalkerSettings? _settings;

    public static string? WalkerBaseAddress => Environment.GetEnvironmentVariable("WalkerBaseUrl");

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(WalkerBaseAddress);

    public WalkerSettings Settings =>
        _settings ?? throw new InvalidOperationException("Walker settings are not available until InitializeAsync has run.");

    public async ValueTask InitializeAsync()
    {
        if (!IsConfigured)
        {
            return;
        }

        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        _playwrightSettings = PlaywrightSettings.Read(configuration);
        _settings = WalkerSettings.Read(configuration);

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = _playwrightSettings.Headless });
    }

    public async Task<(IAsyncDisposable Context, IPage Page)> NewPageAsync(PlaywrightSuite suite)
    {
        var browser = _browser
            ?? throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");
        var playwrightSettings = _playwrightSettings
            ?? throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");
        var baseAddress = WalkerBaseAddress
            ?? throw new InvalidOperationException("WalkerBaseUrl is not set.");

        var contextOptions = new BrowserNewContextOptions
        {
            BaseURL = baseAddress.TrimEnd('/'),
            IgnoreHTTPSErrors = true,
            UserAgent =
                $"{Settings.BrowserUserAgent} {Telemetry.Metrics.SyntheticUserAgentToken}/{Settings.SyntheticUserAgentVersion}",
        };

        return await PlaywrightArtifactRecorder.CreateSessionAsync(
            browser, PlaywrightFixture.AppArtifactName, suite.ToString(), contextOptions, playwrightSettings);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }
}
