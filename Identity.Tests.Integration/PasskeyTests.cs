namespace Identity.Tests.Integration;

using Identity.Pages.Account.Manage;
using Identity.Tests.Integration.Infrastructure;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class PasskeyTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task PasskeyManagePage_RendersSubmitButton_NotGenericElement()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();
        using var client = await AccountSession.SignedInClientAsync(fixture, email, password);

        var page = await client.GetStringAsync("/Account/Manage/Passkeys", TestContext.Current.CancellationToken);

        Assert.Contains(PasskeySubmitTagHelper.ButtonOpeningTag, page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginPage_RendersPasskeySubmitButton_NotGenericElement()
    {
        using var client = AccountSession.NewClient(fixture);

        var page = await client.GetStringAsync(PageRoutes.Login, TestContext.Current.CancellationToken);

        Assert.Contains(PasskeySubmitTagHelper.ButtonOpeningTag, page, StringComparison.Ordinal);
    }
}
