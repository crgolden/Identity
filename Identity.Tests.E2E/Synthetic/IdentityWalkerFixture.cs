namespace Identity.Tests.E2E.Synthetic;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

public sealed class IdentityWalkerFixture : IAsyncLifetime
{
    private const string SyntheticUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 crgolden-synthetic/1.0";

    private static readonly bool Headless =
        !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED"), "1", StringComparison.OrdinalIgnoreCase);

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public static string? WalkerBaseAddress => Environment.GetEnvironmentVariable("WalkerBaseUrl");

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(WalkerBaseAddress);

    public async ValueTask InitializeAsync()
    {
        if (!IsConfigured)
        {
            return;
        }

        var exitCode = Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright install failed with exit code {exitCode}.");
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = Headless });
    }

    public async Task<(IAsyncDisposable Context, IPage Page)> NewPageAsync(string suiteName)
    {
        var browser = _browser
            ?? throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");
        var baseAddress = WalkerBaseAddress
            ?? throw new InvalidOperationException("WalkerBaseUrl is not set.");

        var contextOptions = new BrowserNewContextOptions
        {
            BaseURL = baseAddress.TrimEnd('/'),
            IgnoreHTTPSErrors = true,
            UserAgent = SyntheticUserAgent,
        };

        var (session, page) = await PlaywrightArtifactRecorder.CreateSessionAsync(browser, "Identity", suiteName, contextOptions);
        page.SetDefaultTimeout(60_000);
        return (session, page);
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