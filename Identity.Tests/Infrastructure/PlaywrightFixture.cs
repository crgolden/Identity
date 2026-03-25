namespace Identity.Tests.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

/// <summary>
/// xUnit collection fixture that owns the <see cref="IdentityWebApplicationFactory"/>,
/// the Playwright browser, and provides per-test page creation.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private static readonly bool CI = bool.TryParse(Environment.GetEnvironmentVariable("CI"), out var isCi) && isCi;
    private static readonly bool Headless = !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED"), "1", StringComparison.OrdinalIgnoreCase);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PlaywrightFixture()
    {
        Factory = new IdentityWebApplicationFactory();
        BaseAddress = string.Empty;
        SharedEmail = $"e2e-shared-{Guid.NewGuid()}@test.invalid";
        SharedPassword = $"Test@{Guid.NewGuid():N}!A1";
    }

    public IdentityWebApplicationFactory Factory { get; }

    public EmailCaptureService Email => Factory.EmailCapture;

    public string BaseAddress { get; private set; }

    /// <summary>
    /// Email of a pre-confirmed user created during fixture initialization.
    /// Use with <see cref="SharedPassword"/> in tests that only need an authenticated session
    /// and don't care about the specific user identity.
    /// </summary>
    public string SharedEmail { get; }

    /// <summary>Password for the pre-confirmed user at <see cref="SharedEmail"/>.</summary>
    public string SharedPassword { get; }

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
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless
        });

        // Warm up the server so connection pool / IdentityServer keys are ready
        var (warmupCtx, warmupPage) = await NewPageAsync();
        await using (warmupCtx)
        {
            await warmupPage.GotoAsync("/Account/Login");
        }

        // Pre-create a confirmed user via the UserManager API so tests that only need
        // an authenticated session (GrantsTests, ServerSideSessionsTests) can skip the
        // browser-based registration flow entirely. This avoids those tests accumulating
        // extra DB state late in the suite and reduces the chance of login timeouts.
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var sharedUser = new IdentityUser<Guid>
        {
            UserName = SharedEmail,
            Email = SharedEmail,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(sharedUser, SharedPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create shared test user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    /// Creates a confirmed user directly via <see cref="UserManager{TUser}"/> (no browser flow).
    /// Use in tests that need a pre-existing authenticated user but do not test the registration UI.
    /// This is orders of magnitude faster than the browser-based registration flow.
    /// </summary>
    /// <returns>A task that resolves to the new user's email and password.</returns>
    public async Task<(string Email, string Password)> CreateConfirmedUserAsync()
    {
        const string password = "Test@123456!";
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = new IdentityUser<Guid>
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return (email, password);
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

        // CI machines are slower; double the default 30s timeout to avoid intermittent failures
        // on late-running tests when the machine is under load.
        if (CI)
        {
            page.SetDefaultTimeout(60_000);
        }

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
        if (CI)
        {
            await CleanupDatabaseAsync();
        }

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