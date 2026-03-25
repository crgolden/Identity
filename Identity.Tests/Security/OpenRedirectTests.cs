namespace Identity.Tests.Security;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class OpenRedirectTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Login_WithAbsoluteReturnUrl_DoesNotRedirectExternally()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login?ReturnUrl=https%3A%2F%2Fevil.com");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

            Assert.DoesNotContain("evil.com", new Uri(page.Url).Host, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Login_WithProtocolRelativeReturnUrl_DoesNotRedirectExternally()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login?ReturnUrl=%2F%2Fevil.com");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

            Assert.DoesNotContain("evil.com", new Uri(page.Url).Host, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Login_WithValidLocalReturnUrl_Succeeds()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login?ReturnUrl=%2FAccount%2FManage");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            Assert.DoesNotContain("/Account/Login", page.Url);
        }
    }
}
