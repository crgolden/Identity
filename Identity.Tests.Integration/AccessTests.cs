namespace Identity.Tests.Integration;

using Identity.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class AccessTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Admin_Unauthenticated_Redirects_To_Login()
    {
        using var client = AccountSession.NewClient(fixture);

        using var response = await client.GetAsync(AuthorizationNames.AdminFolder, TestContext.Current.CancellationToken);

        Assert.Equal(PageRoutes.Login, AccountSession.LocationPath(client, response));
    }

    [Fact]
    public async Task Manage_Unauthenticated_Redirects_To_Login()
    {
        using var client = AccountSession.NewClient(fixture);

        using var response = await client.GetAsync("/Account/Manage", TestContext.Current.CancellationToken);

        Assert.Equal(PageRoutes.Login, AccountSession.LocationPath(client, response));
    }

    [Fact]
    public async Task Admin_NonAdminRole_Redirects_To_AccessDenied()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();
        using var client = await AccountSession.SignedInClientAsync(fixture, email, password);

        var accessDeniedPath = fixture.Factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme).AccessDeniedPath;

        using var response = await client.GetAsync(AuthorizationNames.AdminFolder, TestContext.Current.CancellationToken);

        Assert.Equal(accessDeniedPath.Value, AccountSession.LocationPath(client, response));
    }
}
