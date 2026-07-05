namespace Identity.Tests.E2E.Infrastructure;

using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public sealed class TestClientHelper(PlaywrightFixture fixture)
{
    public async Task<string> SeedConsentClientAsync(string redirectUri = "https://localhost:9999/callback")
    {
        var clientId = $"test-{Guid.NewGuid():N}";

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var openidResource = await db.IdentityResources.FirstOrDefaultAsync(r => r.Name == "openid");
        if (openidResource == null)
        {
            db.IdentityResources.Add(new IdentityResource
            {
                Name = "openid",
                DisplayName = "Your user identifier",
                Required = false,
                UserClaims = [new() { Type = "sub" }],
            });
        }
        else if (openidResource.Required)
        {
            openidResource.Required = false;
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
