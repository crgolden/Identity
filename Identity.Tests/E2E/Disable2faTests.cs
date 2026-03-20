namespace Identity.Tests.E2E;

using Infrastructure;
using OtpNet;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class Disable2faTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Disable2fa_AfterSetup_SubsequentLogin_DoesNotRequire2fa()
    {
        var (email, password) = await CreateConfirmedUserAsync();

        // Capture the shared key outside the setup context so it can be reused in the disable step.
        var capturedSharedKey = string.Empty;

        // --- Setup TOTP ---
        var (setupCtx, setupPage) = await fixture.NewPageAsync();
        await using (setupCtx)
        {
            await setupPage.GotoAsync("/Account/Login");
            await setupPage.FillAsync("input[name='Input.Email']", email);
            await setupPage.FillAsync("input[name='Input.Password']", password);
            await setupPage.ClickAsync("button[type='submit']");
            await setupPage.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            await setupPage.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await setupPage.ClickAsync("a[href*='EnableAuthenticator']");
            await setupPage.WaitForURLAsync("**/Account/Manage/EnableAuthenticator**");

            var sharedKeyEl = setupPage.Locator("kbd");
            capturedSharedKey = (await sharedKeyEl.First.TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            var keyBytes = Base32Encoding.ToBytes(capturedSharedKey);
            var totp = new Totp(keyBytes);
            var code = totp.ComputeTotp();

            await setupPage.FillAsync("input[name='Input.Code']", code);
            await setupPage.ClickAsync("button.btn-primary");

            await setupPage.WaitForURLAsync(url =>
                url.Contains("ShowRecoveryCodes") || url.Contains("TwoFactorAuthentication"));
        }

        // --- Verify 2FA is required at login ---
        var (loginCtx, loginPage) = await fixture.NewPageAsync();
        await using (loginCtx)
        {
            await loginPage.GotoAsync("/Account/Login");
            await loginPage.FillAsync("input[name='Input.Email']", email);
            await loginPage.FillAsync("input[name='Input.Password']", password);
            await loginPage.ClickAsync("button[type='submit']");
            await loginPage.WaitForURLAsync("**/Account/LoginWith2fa**");
        }

        // --- Disable 2FA ---
        var (disableCtx, disablePage) = await fixture.NewPageAsync();
        await using (disableCtx)
        {
            // Login with password — 2FA is now required
            await disablePage.GotoAsync("/Account/Login");
            await disablePage.FillAsync("input[name='Input.Email']", email);
            await disablePage.FillAsync("input[name='Input.Password']", password);
            await disablePage.ClickAsync("button[type='submit']");
            await disablePage.WaitForURLAsync("**/Account/LoginWith2fa**");

            // Complete 2FA using the shared key captured during setup
            var disableKeyBytes = Base32Encoding.ToBytes(capturedSharedKey);
            var disableTotp = new Totp(disableKeyBytes);
            var disableCode = disableTotp.ComputeTotp();
            await disablePage.FillAsync("input[name='Input.TwoFactorCode']", disableCode);
            await disablePage.ClickAsync("button[type='submit']");
            await disablePage.WaitForURLAsync(url => !url.Contains("/Account/LoginWith2fa"));

            // Navigate to Disable2fa and confirm
            await disablePage.GotoAsync("/Account/Manage/Disable2fa");
            await disablePage.WaitForURLAsync("**/Account/Manage/Disable2fa**");
            await disablePage.ClickAsync("button.btn-danger");
            await disablePage.WaitForURLAsync("**/Account/Manage/TwoFactorAuthentication**");
        }

        // --- Verify 2FA is no longer required ---
        var (verifyCtx, verifyPage) = await fixture.NewPageAsync();
        await using (verifyCtx)
        {
            await verifyPage.GotoAsync("/Account/Login");
            await verifyPage.FillAsync("input[name='Input.Email']", email);
            await verifyPage.FillAsync("input[name='Input.Password']", password);
            await verifyPage.ClickAsync("button[type='submit']");

            // Should land somewhere other than the 2FA challenge page
            await verifyPage.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("LoginWith2fa", verifyPage.Url);
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

            var confirmEmail = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureService.ExtractLink(confirmEmail.HtmlBody, "http");
            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        return (email, password);
    }
}
