namespace Identity.Tests.E2E.Infrastructure;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.CAPTCHA;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static System.String;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    internal const string AppArtifactName = nameof(Identity);

    private const string SeededClientId = "e2e-admin-client";
    private const string SeededApiResourceName = "e2e-api-resource";
    private const string SeededApiScopeName = "e2e-api-scope";
    private const string SeededIdentityResourceName = "e2e-identity-resource";
    private static readonly bool StrykerActive = Environment.GetEnvironmentVariable("STRYKER_MUTANT_FILE") is not null;
    private readonly IdentityWebApplicationFactory _factory = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _baseAddress;
    private PlaywrightSettings? _playwrightSettings;
    private E2ESettings? _settings;
    private Uri? _recaptchaScriptEndpoint;
    private bool _started;

    public IdentityWebApplicationFactory Factory => _factory;

    public E2ESettings Settings =>
        _settings ?? throw new InvalidOperationException("Settings are not available until InitializeAsync has run.");

    public EmailCaptureSender Email => _factory.EmailCapture;

    public string BaseAddress =>
        _baseAddress ?? throw new InvalidOperationException("BaseAddress is not available until InitializeAsync has run.");

    public string ExtractEmailLink(string htmlBody) =>
        EmailCaptureSender.ExtractLink(htmlBody, Factory.Services.GetRequiredService<AccountEmailSettings>());

    public async ValueTask InitializeAsync()
    {
        if (StrykerActive)
        {
            return;
        }

        Factory.CreateClient();
        _baseAddress = Factory.ServerAddress;
        var configuration = Factory.Services.GetRequiredService<IConfiguration>();
        _playwrightSettings = PlaywrightSettings.Read(configuration);
        _settings = E2ESettings.Read(configuration);
        _recaptchaScriptEndpoint = Factory.Services.GetRequiredService<ICAPTCHAService>().ScriptEndpoint
            ?? throw new InvalidOperationException($"{nameof(ICAPTCHAService.ScriptEndpoint)} is not configured.");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _playwrightSettings.Headless
        });

        var (warmupCtx, warmupPage) = await NewPageAsync();
        await using (warmupCtx)
        {
            await warmupPage.GotoAsync(PageRoutes.Login);
        }

        _started = true;
    }

    public async Task<(string Email, string Password)> CreateConfirmedUserAsync()
    {
        var password = Generated.NewPassword();
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
        var password = Generated.NewPassword();
        var email = $"e2e-admin-{Guid.NewGuid()}@test.invalid";

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(AuthorizationNames.AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(AuthorizationNames.AdminRole));
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

        var addRoleResult = await userManager.AddToRoleAsync(user, AuthorizationNames.AdminRole);
        if (!addRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                Join(", ", addRoleResult.Errors.Select(e => e.Description)));
        }

        return (email, password);
    }

    public async Task<int> SeedClientAsync(string clientId = SeededClientId)
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
            ClientName = Generated.NewDisplayName(),
            ProtocolType = OidcStandardConstants.OidcProtocol,
            RequireClientSecret = false
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client.Id;
    }

    public async Task<int> SeedApiResourceAsync(string name = SeededApiResourceName)
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
            DisplayName = Generated.NewDisplayName()
        };
        db.ApiResources.Add(apiResource);
        await db.SaveChangesAsync();
        return apiResource.Id;
    }

    public async Task<int> SeedApiScopeAsync(string name = SeededApiScopeName)
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
            DisplayName = Generated.NewDisplayName()
        };
        db.ApiScopes.Add(apiScope);
        await db.SaveChangesAsync();
        return apiScope.Id;
    }

    public async Task<int> SeedIdentityResourceAsync(string name = SeededIdentityResourceName)
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
            DisplayName = Generated.NewDisplayName()
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

    public async Task<TValue> GetSingleAsync<TEntity, TValue>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TValue>> selector)
        where TEntity : class
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<TEntity>().Where(predicate).Select(selector).SingleAsync();
    }

    public async Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<TEntity>().AnyAsync(predicate);
    }

    public async Task<int> CountAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<TEntity>().CountAsync(predicate);
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

    public async Task<(IAsyncDisposable Context, IPage Page)> NewPageAsync(PlaywrightSuite suite = PlaywrightSuite.E2E)
    {
        if (_browser is null || _playwrightSettings is null || _recaptchaScriptEndpoint is null)
        {
            throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");
        }

        var contextOptions = new BrowserNewContextOptions
        {
            BaseURL = BaseAddress,
            IgnoreHTTPSErrors = true
        };
        var (session, page) = await PlaywrightArtifactRecorder.CreateSessionAsync(
            _browser, AppArtifactName, suite.ToString(), contextOptions, _playwrightSettings);

        await page.Context.AddInitScriptAsync(BrowserScripts.GrecaptchaStub);
        await page.Context.RouteAsync($"{_recaptchaScriptEndpoint}**", route => route.AbortAsync());

        return (session, page);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if (_started)
        {
            await CleanupDatabaseAsync();
        }

        await _factory.DisposeAsync();
    }

    private async Task CleanupDatabaseAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!db.Database.GetDbConnection().Database.EndsWith(Settings.TestCatalogSuffix, StringComparison.Ordinal))
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
