namespace Identity.Tests.E2E.Infrastructure;

using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static String;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    private static readonly bool CI = bool.TryParse(Environment.GetEnvironmentVariable("CI"), out var isCi) && isCi;
    private static readonly bool Headless = !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED"), "1", StringComparison.OrdinalIgnoreCase);
    private static readonly bool StrykerActive = Environment.GetEnvironmentVariable("STRYKER_MUTANT_FILE") is not null;
    private readonly IdentityWebApplicationFactory _factory = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _baseAddress;
    private bool _started;

    public IdentityWebApplicationFactory Factory => _factory;

    public EmailCaptureSender Email => _factory.EmailCapture;

    public string BaseAddress =>
        _baseAddress ?? throw new InvalidOperationException("BaseAddress is not available until InitializeAsync has run.");

    public async ValueTask InitializeAsync()
    {
        if (StrykerActive)
        {
            return;
        }

        Factory.CreateClient();
        _baseAddress = Factory.ServerAddress;

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

        var (warmupCtx, warmupPage) = await NewPageAsync();
        await using (warmupCtx)
        {
            await warmupPage.GotoAsync("/Account/Login");
        }

        _started = true;
    }

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
                Join(", ", result.Errors.Select(e => e.Description)));
        }

        return (email, password);
    }

    public async Task<(string Email, string Password)> CreateAdminUserAsync()
    {
        const string password = "Test@Admin123!";
        var email = $"e2e-admin-{Guid.NewGuid()}@test.invalid";

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        var user = new IdentityUser<Guid>
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, "Admin");
        if (!addRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                Join(", ", addRoleResult.Errors.Select(e => e.Description)));
        }

        return (email, password);
    }

    public async Task<int> SeedClientAsync(string clientId = "e2e-admin-client")
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await db.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
        if (existing is not null)
        {
            return existing.Id;
        }

        var client = new Client
        {
            ClientId = clientId,
            ClientName = "E2E Admin Test Client",
            ProtocolType = "oidc",
            RequireClientSecret = false,
            AllowOfflineAccess = false
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client.Id;
    }

    public async Task<int> SeedApiResourceAsync(string name = "e2e-api-resource")
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await db.ApiResources.FirstOrDefaultAsync(r => r.Name == name);
        if (existing is not null)
        {
            return existing.Id;
        }

        var apiResource = new ApiResource
        {
            Name = name,
            DisplayName = "E2E API Resource"
        };
        db.ApiResources.Add(apiResource);
        await db.SaveChangesAsync();
        return apiResource.Id;
    }

    public async Task<int> SeedApiScopeAsync(string name = "e2e-api-scope")
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await db.ApiScopes.FirstOrDefaultAsync(s => s.Name == name);
        if (existing is not null)
        {
            return existing.Id;
        }

        var apiScope = new ApiScope
        {
            Name = name,
            DisplayName = "E2E API Scope"
        };
        db.ApiScopes.Add(apiScope);
        await db.SaveChangesAsync();
        return apiScope.Id;
    }

    public async Task<int> SeedIdentityResourceAsync(string name = "e2e-identity-resource")
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await db.IdentityResources.FirstOrDefaultAsync(r => r.Name == name);
        if (existing is not null)
        {
            return existing.Id;
        }

        var identityResource = new IdentityResource
        {
            Name = name,
            DisplayName = "E2E Identity Resource"
        };
        db.IdentityResources.Add(identityResource);
        await db.SaveChangesAsync();
        return identityResource.Id;
    }

    public async Task<Guid> GetRoleIdAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var role = await roleManager.FindByNameAsync(roleName)
            ?? throw new InvalidOperationException($"Role '{roleName}' not found.");
        return role.Id;
    }

    public async Task<string> GetPersistedGrantKeyAsync(string clientId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var grant = await db.PersistedGrants.FirstOrDefaultAsync(g => g.ClientId == clientId)
            ?? throw new InvalidOperationException($"No persisted grant found for client '{clientId}'.");
        return grant.Key;
    }

    public async Task ConfirmUserEmailAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"User '{email}' not found.");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<Guid> GetUserIdAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"User '{email}' not found.");
        return user.Id;
    }

    public async Task DeleteUserIfExistsAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            await userManager.DeleteAsync(user);
        }
    }

    public async Task<(IAsyncDisposable Context, IPage Page)> NewPageAsync(string suiteName = "E2E")
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");
        }

        var contextOptions = new BrowserNewContextOptions
        {
            BaseURL = BaseAddress,
            IgnoreHTTPSErrors = true
        };
        var (session, page) = await PlaywrightArtifactRecorder.CreateSessionAsync(_browser, "Identity", suiteName, contextOptions);

        await page.Context.AddInitScriptAsync("window.grecaptcha = { ready: cb => cb(), execute: () => Promise.resolve('e2e-test-token') };");
        await page.Context.RouteAsync("https://www.google.com/recaptcha/**", route => route.AbortAsync());

        if (CI)
        {
            page.SetDefaultTimeout(60_000);
        }

        return (session, page);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if (CI && _started)
        {
            await CleanupDatabaseAsync();
        }

        await _factory.DisposeAsync();
    }

    private async Task CleanupDatabaseAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!db.Database.GetDbConnection().Database.EndsWith("Test", StringComparison.Ordinal))
        {
            return;
        }

        await db.Users.ExecuteDeleteAsync();
        await db.Roles.ExecuteDeleteAsync();
        await db.PersistedGrants.ExecuteDeleteAsync();
        await db.DeviceFlowCodes.ExecuteDeleteAsync();
        await db.Keys.ExecuteDeleteAsync();
        await db.ServerSideSessions.ExecuteDeleteAsync();
        await db.PushedAuthorizationRequests.ExecuteDeleteAsync();
        await db.Clients.ExecuteDeleteAsync();
        await db.IdentityResources.ExecuteDeleteAsync();
        await db.ApiResources.ExecuteDeleteAsync();
        await db.ApiScopes.ExecuteDeleteAsync();
        await db.IdentityProviders.ExecuteDeleteAsync();
    }
}