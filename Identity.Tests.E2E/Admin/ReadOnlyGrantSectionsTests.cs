namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// Covers Admin-E2E-Guide.md's RO2/RO3 scenarios (Details shows fields, Delete removes record) for the
// one of the eight read-only grant-store sections that has a realistic, real production path via the
// existing app: PersistedGrants (via the same authorize+consent+allow flow ConsentTests.cs already
// drives). The other seven stay at the Index-loads-empty-ok coverage already in AdminTests.cs, by
// explicit decision:
// - ServerSideSessions: confirmed by reading Program.cs's .AddIdentityServer(...) chain that
//   .AddServerSideSessions() is never called (grepped the whole repo -- zero matches outside docs/
//   tests), and confirmed empirically -- a WP5 run that logged in twice against a running instance
//   produced one SELECT against [ServerSideSessions] and zero INSERTs. There is no code path in this
//   app that populates this table, so it belongs in the same bucket as the disabled SAML sections below,
//   not with PersistedGrants.
// - DeviceFlowCodes / PushedAuthorizationRequests: no existing UI-driven flow produces a row; building
//   one would need meaningfully more scaffolding (raw device-authorization / PAR requests) for a single
//   test's worth of value.
// - SamlSigninStates / SamlLogoutSessions / SamlLogoutSessionRequestIndices: confirmed via the live
//   Duende diagnostics dump that this app's SAML endpoints are disabled
//   (EnableSamlSigninEndpoint/EnableSamlLogoutEndpoint/etc. all false) -- there is no code path in this
//   app that can ever populate these tables, so fabricating rows would test something that can't happen
//   in practice.
//
// Dynamically-keyed row action ids (details-{key}/delete-{key}) can't be predicted client-side, so this
// test locates the row by matching real page TEXT content (the Client column) then selects within that
// row via the wildcard-id pattern already established in AdminTests.cs's Users/Roles tests
// ([id^='details-']) -- this is distinct from the earlier collection-editor id fix, which targets an
// input's `value` attribute (never valid for HasText); here the target text is genuinely rendered as
// visible cell content.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ReadOnlyGrantSectionsTests(PlaywrightFixture fixture)
{
    private const string RedirectUri = "https://localhost:9999/callback";

    [Fact]
    public async Task PersistedGrants_Details_Shows_Fields_And_Delete_Removes_Record()
    {
        var (adminEmail, adminPassword) = await fixture.CreateAdminUserAsync();
        var clientId = await ProduceAuthorizationCodeGrantAsync(fixture);

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, adminEmail, adminPassword);
            await page.GotoAsync("/Admin/PersistedGrants");

            var row = page.Locator("tr", new PageLocatorOptions { HasText = clientId });
            await row.Locator("[id^='details-']").First.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/PersistedGrants/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(clientId)).ToBeVisibleAsync();

            await page.ClickAsync("#btn-delete");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Delete");
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.GotoAsync("/Admin/PersistedGrants");
            await Assertions.Expect(page.GetByText(clientId)).Not.ToBeVisibleAsync();
        }
    }

    private static async Task<string> ProduceAuthorizationCodeGrantAsync(PlaywrightFixture fixture)
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync(RedirectUri);
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(BuildAuthorizeUrl(clientId, RedirectUri, "e2e-grant-state"));

            if (page.Url.Contains("localhost:9999"))
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
                r => r.Url.Contains("localhost:9999"),
                new PageRunAndWaitForRequestOptions { Timeout = 15_000 });
        }

        return clientId;
    }

    private static string BuildAuthorizeUrl(string clientId, string redirectUri, string state) =>
        $"/connect/authorize?client_id={Uri.EscapeDataString(clientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        "&response_type=code" +
        "&scope=openid" +
        $"&state={Uri.EscapeDataString(state)}";

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/Account/Login");
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }
}
