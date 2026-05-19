namespace Identity.Tests.E2E;

using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ConsentTests(PlaywrightFixture fixture)
{
    private const string RedirectUri = "https://localhost:9999/callback";

    [Fact]
    public async Task Consent_Deny_RedirectsWithAccessDenied()
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync(RedirectUri);
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(BuildAuthorizeUrl(clientId, RedirectUri, "deny-state"));

            if (page.Url.Contains("localhost:9999"))
            {
                Assert.Contains("error=access_denied", page.Url);
                return;
            }

            Assert.Contains("/Account/Manage/Consent", page.Url);

            // Capture the redirect URL from the browser request event before ERR_CONNECTION_REFUSED
            var request = await page.RunAndWaitForRequestAsync(
                async () => await page.ClickAsync("#consent-deny"),
                r => r.Url.Contains("localhost:9999"));

            Assert.Contains("error=access_denied", request.Url);
        }
    }

    [Fact]
    public async Task Consent_Allow_RedirectsWithCode()
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync(RedirectUri);
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(BuildAuthorizeUrl(clientId, RedirectUri, "allow-state"));

            if (page.Url.Contains("localhost:9999"))
            {
                Assert.Contains("code=", page.Url);
                return;
            }

            Assert.Contains("/Account/Manage/Consent", page.Url);

            // Check all scope checkboxes so the scopes are included in the form submission.
            var checkboxes = await page.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])");
            foreach (var checkbox in checkboxes)
            {
                await checkbox.CheckAsync();
            }

            // Capture the redirect URL from the browser request event before ERR_CONNECTION_REFUSED.
            var request = await page.RunAndWaitForRequestAsync(
                async () => await page.ClickAsync("#consent-allow"),
                r => r.Url.Contains("localhost:9999"),
                new PageRunAndWaitForRequestOptions { Timeout = 15_000 });

            Assert.Contains("code=", request.Url);
        }
    }

    [Fact]
    public async Task Consent_NoScopesConsented_ShowsError()
    {
        var helper = new TestClientHelper(fixture);
        var clientId = await helper.SeedConsentClientAsync(RedirectUri);
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(BuildAuthorizeUrl(clientId, RedirectUri, "noscope-state"));

            if (!page.Url.Contains("/Account/Manage/Consent"))
            {
                return;
            }

            // Uncheck all non-required scopes
            var checkboxes = await page.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])");
            foreach (var checkbox in checkboxes)
            {
                if (await checkbox.IsCheckedAsync())
                {
                    await checkbox.UncheckAsync();
                }
            }

            await page.RunAndWaitForResponseAsync(
                () => page.ClickAsync("#consent-allow"),
                r => r.Url.Contains("/Account/Manage/Consent") && r.Request.Method == "POST");

            Assert.Contains("/Account/Manage/Consent", page.Url);
            var content = await page.ContentAsync();
            Assert.Contains("permission", content, StringComparison.OrdinalIgnoreCase);
        }
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
        await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
    }
}
