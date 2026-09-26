namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ReadOnlyGrantSectionsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task PersistedGrants_Details_Shows_Fields_And_Delete_Removes_Record()
    {
        var (adminEmail, adminPassword) = await fixture.CreateAdminUserAsync();
        var clientId = await ProduceAuthorizationCodeGrantAsync(fixture);

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, adminEmail, adminPassword);
            await page.GotoAsync("/Admin/PersistedGrants");

            var grantKey = await fixture.GetPersistedGrantKeyAsync(clientId);
            await page.ClickAsync($"[id='details-{grantKey}']");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/PersistedGrants/Details"));
            await Assertions.Expect(page.Locator("#grant-client-id")).ToBeVisibleAsync();

            await page.ClickAsync("#btn-delete");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await page.GotoAsync("/Admin/PersistedGrants");
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator($"[id='details-{grantKey}']")).ToHaveCountAsync(0);
        }
    }

    private static async Task<string> ProduceAuthorizationCodeGrantAsync(PlaywrightFixture fixture)
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync();
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(AuthorizeRequest.Url(clientId, OidcStandardConstants.LoopbackRedirectUri, Generated.NewClaimValue()));

            if (page.Url.Contains(OidcStandardConstants.LoopbackHost, StringComparison.Ordinal))
            {
                return clientId;
            }

            var checkboxes = await page.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])");
            foreach (var checkbox in checkboxes)
            {
                await checkbox.CheckAsync();
            }

            await page.RunAndWaitForRequestAsync(
                async () => await page.ClickAsync("#consent-allow"),
                r => r.Url.Contains(OidcStandardConstants.LoopbackHost, StringComparison.Ordinal));
        }

        return clientId;
    }

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(PageRoutes.Login);
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
    }
}
