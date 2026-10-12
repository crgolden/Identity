namespace Identity.Tests.Integration;

using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Pages;
using Identity.Tests.E2E.Infrastructure;
using Identity.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class ErrorPageTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task AuthorizeCallback_UnknownSignInRequest_ErrorPageOffersToContinueToTheClient()
    {
        var redirectOrigin = Generated.NewOrigin();
        var clientId = await SeedClientAsync(redirectOrigin);
        var (email, password) = await fixture.CreateConfirmedUserAsync();
        using var client = await AccountSession.SignedInClientAsync(fixture, email, password);
        var unknownRequestReference = Guid.NewGuid().ToString("N");
        var deadCallback = QueryHelpers.AddQueryString(
            OidcStandardConstants.AuthorizeCallbackPath,
            new Dictionary<string, string?>
            {
                [OidcStandardConstants.ClientIdParameter] = clientId,
                [OidcStandardConstants.RequestUriParameter] = $"{OidcStandardConstants.PushedAuthorizationRequestUriPrefix}:{unknownRequestReference}",
            });

        using var callbackResponse = await client.GetAsync(deadCallback, TestContext.Current.CancellationToken);
        Assert.Equal(PageRoutes.Error, AccountSession.LocationPath(client, callbackResponse));
        var errorPage = await client.GetStringAsync(callbackResponse.Headers.Location, TestContext.Current.CancellationToken);

        var continueHref = AccountSession.ReadValue(errorPage, HtmlFormConstants.AnchorHrefByIdPattern, Error.ContinueToClientLinkId);
        Assert.Equal(new Uri(redirectOrigin), new Uri(continueHref));
    }

    private async Task<string> SeedClientAsync(string redirectOrigin)
    {
        var clientId = Generated.NewClientIdentifier();
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Clients.Add(new Client
        {
            ClientId = clientId,
            ClientName = Generated.NewClientName(),
            ProtocolType = OidcStandardConstants.OidcProtocol,
            RequireClientSecret = false,
            AllowedGrantTypes = [new() { GrantType = OidcStandardConstants.AuthorizationCodeGrant }],
            RedirectUris = [new() { RedirectUri = $"{redirectOrigin}/{Generated.NewToken()}" }],
            AllowedScopes = [new() { Scope = OidcStandardConstants.OpenIdScope }],
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return clientId;
    }
}
