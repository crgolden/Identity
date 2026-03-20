namespace Identity.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

/// <summary>
/// xUnit collection fixture that owns the <see cref="IdentityWebApplicationFactory"/>,
/// the Playwright browser, and provides per-test page creation.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PlaywrightFixture()
    {
        Factory = new IdentityWebApplicationFactory();
        BaseAddress = string.Empty;
    }

    public IdentityWebApplicationFactory Factory { get; }

    public EmailCaptureService Email => Factory.EmailCapture;

    public string BaseAddress { get; private set; }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        Factory.CreateClient(); // Triggers server startup; populates Factory.ServerAddress.
        BaseAddress = Factory.ServerAddress;

        var exitCode = Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright install failed with exit code {exitCode}.");
        }

        _playwright = await Playwright.CreateAsync();
        var headless = !string.Equals(
            Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED"),
            "1",
            StringComparison.OrdinalIgnoreCase);
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless
        });

        // Warm up the server so connection pool / IdentityServer keys are ready
        var (warmupCtx, warmupPage) = await NewPageAsync();
        await using (warmupCtx)
        {
            await warmupPage.GotoAsync("/Account/Login");
        }
    }

    /// <summary>Creates a new Playwright browser context and page configured with <see cref="BaseAddress"/>.</summary>
    /// <returns>A task that resolves to a tuple of the browser context and page.</returns>
    public async Task<(IBrowserContext Context, IPage Page)> NewPageAsync()
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");
        }

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseAddress,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        return (context, page);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        await CleanupDatabaseAsync();
        await Factory.DisposeAsync();
    }

    private async Task CleanupDatabaseAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Identity — cascade deletes UserClaims, UserLogins, UserTokens, UserPasskeys, UserRoles
        await db.Users.ExecuteDeleteAsync();

        // Roles — cascade deletes RoleClaims (UserRoles already gone via Users cascade)
        await db.Roles.ExecuteDeleteAsync();

        // IdentityServer operational (no FK dependencies)
        await db.PersistedGrants.ExecuteDeleteAsync();
        await db.DeviceFlowCodes.ExecuteDeleteAsync();
        await db.Keys.ExecuteDeleteAsync();
        await db.ServerSideSessions.ExecuteDeleteAsync();
        await db.PushedAuthorizationRequests.ExecuteDeleteAsync();

        // IdentityServer config — cascade deletes all child tables
        await db.Clients.ExecuteDeleteAsync();
        await db.IdentityResources.ExecuteDeleteAsync();
        await db.ApiResources.ExecuteDeleteAsync();
        await db.ApiScopes.ExecuteDeleteAsync();
        await db.IdentityProviders.ExecuteDeleteAsync();
    }
}