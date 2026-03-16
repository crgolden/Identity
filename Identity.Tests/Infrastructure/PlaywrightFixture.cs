using Microsoft.Playwright;

namespace Identity.Tests.Infrastructure;

/// <summary>
/// xUnit collection fixture that owns the <see cref="IdentityWebApplicationFactory"/>,
/// the Playwright browser, and provides per-test page creation.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IdentityWebApplicationFactory Factory { get; }
    public EmailCaptureService Email => Factory.EmailCapture;

    /// <summary>Base address of the in-process test server.</summary>
    public string BaseAddress { get; private set; }

    public PlaywrightFixture()
    {
        Factory = new IdentityWebApplicationFactory();
        BaseAddress = string.Empty;
    }

    public async ValueTask InitializeAsync()
    {
        var client = Factory.CreateClient();
        var baseAddress = client.BaseAddress
            ?? throw new InvalidOperationException("WebApplicationFactory did not set a base address on the HttpClient.");
        BaseAddress = baseAddress.ToString().TrimEnd('/');

        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
            throw new InvalidOperationException($"Playwright install failed with exit code {exitCode}.");

        _playwright = await Playwright.CreateAsync();
        var headless = !string.Equals(
            Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED"), "1",
            StringComparison.OrdinalIgnoreCase);
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless
        });
    }

    /// <summary>
    /// Creates a fresh browser context (isolated cookies/storage) and a new page.
    /// The caller is responsible for disposing the context.
    /// </summary>
    public async Task<(IBrowserContext Context, IPage Page)> NewPageAsync()
    {
        if (_browser is null)
            throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseAddress,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        return (context, page);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
        await Factory.DisposeAsync();
    }
}
