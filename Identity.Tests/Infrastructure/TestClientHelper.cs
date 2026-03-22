namespace Identity.Tests.Infrastructure;

using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Helper for seeding Duende IdentityServer client configurations into the database
/// during E2E tests. Clients seeded here are cleaned up when
/// <see cref="PlaywrightFixture.DisposeAsync"/> runs <c>db.Clients.ExecuteDeleteAsync()</c>.
/// </summary>
public sealed class TestClientHelper(PlaywrightFixture fixture)
{
    /// <summary>
    /// Seeds a minimal public OIDC client with <c>RequireConsent = true</c> into the
    /// configuration store and returns its generated <c>clientId</c>.
    /// Also seeds the <c>openid</c> and <c>profile</c> identity resources if they are
    /// not already present.
    /// </summary>
    /// <param name="redirectUri">The redirect URI to register for the client.</param>
    /// <returns>A task resolving to the generated client ID.</returns>
    public async Task<string> SeedConsentClientAsync(string redirectUri = "https://localhost:9999/callback")
    {
        var clientId = $"test-{Guid.NewGuid():N}";

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await db.IdentityResources.AnyAsync(r => r.Name == "openid"))
        {
            db.IdentityResources.Add(new IdentityResource
            {
                Name = "openid",
                DisplayName = "Your user identifier",
                UserClaims = [new() { Type = "sub" }],
            });
        }

        if (!await db.IdentityResources.AnyAsync(r => r.Name == "profile"))
        {
            db.IdentityResources.Add(new IdentityResource
            {
                Name = "profile",
                DisplayName = "User profile",
                UserClaims =
                [
                    new() { Type = "name" },
                    new() { Type = "email" },
                ],
            });
        }

        db.Clients.Add(new Client
        {
            ClientId = clientId,
            ClientName = "E2E Test Client",
            ProtocolType = "oidc",
            RequireConsent = true,
            AllowRememberConsent = true,
            RequireClientSecret = false,
            RequirePkce = false,
            AllowedGrantTypes = [new() { GrantType = "authorization_code" }],
            RedirectUris = [new() { RedirectUri = redirectUri }],
            AllowedScopes = [new() { Scope = "openid" }],
        });

        await db.SaveChangesAsync();
        return clientId;
    }
}
