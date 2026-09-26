namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ConsentTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Consent_Deny_RedirectsWithAccessDenied()
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync();
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(AuthorizeRequest.Url(clientId, OidcStandardConstants.LoopbackRedirectUri, Generated.NewClaimValue()));
            Assert.Contains(PageRoutes.Consent, page.Url, StringComparison.Ordinal);

            var request = await page.RunAndWaitForRequestAsync(
                async () => await page.ClickAsync("#consent-deny"),
                r => r.Url.Contains(OidcStandardConstants.LoopbackHost, StringComparison.Ordinal));

            Assert.Contains("error=access_denied", request.Url, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Consent_Allow_RedirectsWithCode()
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync();
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(AuthorizeRequest.Url(clientId, OidcStandardConstants.LoopbackRedirectUri, Generated.NewClaimValue()));
            Assert.Contains(PageRoutes.Consent, page.Url, StringComparison.Ordinal);

            var checkboxes = await page.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])");
            foreach (var checkbox in checkboxes)
            {
                await checkbox.CheckAsync();
            }

            var request = await page.RunAndWaitForRequestAsync(
                async () => await page.ClickAsync("#consent-allow"),
                r => r.Url.Contains(OidcStandardConstants.LoopbackHost, StringComparison.Ordinal));

            Assert.Contains("code=", request.Url, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Consent_NoScopesConsented_ShowsError()
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync();
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(AuthorizeRequest.Url(clientId, OidcStandardConstants.LoopbackRedirectUri, Generated.NewClaimValue()));

            Assert.Contains(PageRoutes.Consent, page.Url, StringComparison.Ordinal);

            var checkboxes = await page.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])");
            foreach (var checkbox in checkboxes)
            {
                await checkbox.UncheckAsync();
            }

            await page.RunAndWaitForResponseAsync(
                () => page.ClickAsync("#consent-allow"),
                r => r.Url.Contains(PageRoutes.Consent, StringComparison.Ordinal) && string.Equals(r.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal));

            Assert.Contains(PageRoutes.Consent, page.Url, StringComparison.Ordinal);
            await Assertions.Expect(page.Locator("#must-choose-one-error")).ToBeVisibleAsync();
        }
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
