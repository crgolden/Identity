namespace Identity.Tests.E2E.Infrastructure;

using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public sealed class TestClientHelper(PlaywrightFixture fixture)
{
    public async Task<string> SeedConsentClientAsync()
    {
        var clientId = $"test-{Guid.NewGuid():N}";

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var openidResource = await db.IdentityResources.FirstOrDefaultAsync(
            r => r.Name == OidcStandardConstants.OpenIdScope);
        if (openidResource == null)
        {
            db.IdentityResources.Add(new IdentityResource
            {
                Name = OidcStandardConstants.OpenIdScope,
                DisplayName = Generated.NewDisplayName(),
                Required = false,
                UserClaims = [new() { Type = OidcStandardConstants.SubjectClaim }],
            });
        }
        else if (openidResource.Required)
        {
            openidResource.Required = false;
        }

        if (!await db.IdentityResources.AnyAsync(r => r.Name == OidcStandardConstants.ProfileScope))
        {
            db.IdentityResources.Add(new IdentityResource
            {
                Name = OidcStandardConstants.ProfileScope,
                DisplayName = Generated.NewDisplayName(),
                UserClaims =
                [
                    new() { Type = OidcStandardConstants.NameClaim },
                    new() { Type = OidcStandardConstants.EmailClaim },
                ],
            });
        }

        db.Clients.Add(new Client
        {
            ClientId = clientId,
            ClientName = Generated.NewDisplayName(),
            ProtocolType = OidcStandardConstants.OidcProtocol,
            RequireConsent = true,
            RequireClientSecret = false,
            RequirePkce = false,
            AllowedGrantTypes = [new() { GrantType = OidcStandardConstants.AuthorizationCodeGrant }],
            RedirectUris = [new() { RedirectUri = OidcStandardConstants.LoopbackRedirectUri }],
            AllowedScopes = [new() { Scope = OidcStandardConstants.OpenIdScope }],
        });

        await db.SaveChangesAsync();
        return clientId;
    }
}
