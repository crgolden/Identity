namespace Identity.Tests.Integration;

using System.Net;
using Identity.Tests.Integration.Infrastructure;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class GrantsTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Grants_AuthenticatedUser_PageLoads()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();
        using var client = await AccountSession.SignedInClientAsync(fixture, email, password);

        using var response = await client.GetAsync(Pages.Account.Manage.Grants.GrantsPagePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
