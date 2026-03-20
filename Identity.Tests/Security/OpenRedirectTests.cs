namespace Identity.Tests.Security;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class OpenRedirectTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Login_WithAbsoluteReturnUrl_DoesNotRedirectExternally()
    {
        var (email, password) = await CreateConfirmedUserAsync();

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
        var (email, password) = await CreateConfirmedUserAsync();

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
        var (email, password) = await CreateConfirmedUserAsync();

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

    private async Task<(string Email, string Password)> CreateConfirmedUserAsync()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        var (ctx, page) = await fixture.NewPageAsync();
        await using (ctx)
        {
            await page.GotoAsync("/Account/Register");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/Account/RegisterConfirmation**");

            var captured = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureService.ExtractLink(captured.HtmlBody, "http");
            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        return (email, password);
    }
}
