namespace Identity.Tests.E2E;

using Infrastructure;
using OtpNet;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class TwoFactorTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task TwoFactor_Setup_Login_WithTotpCode_Succeeds()
    {
        var (email, password) = await CreateAndLoginAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login first
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Navigate to 2FA setup
            await page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await page.ClickAsync("a[href*='EnableAuthenticator']");
            await page.WaitForURLAsync("**/Account/Manage/EnableAuthenticator**");

            // Extract shared key from page
            var sharedKeyEl = page.Locator("kbd");
            var sharedKey = (await sharedKeyEl.First.TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            // Compute TOTP code
            var keyBytes = Base32Encoding.ToBytes(sharedKey);
            var totp = new Totp(keyBytes);
            var code = totp.ComputeTotp();

            // Submit the code
            await page.FillAsync("input[name='Input.Code']", code);
            await page.ClickAsync("button.btn-primary");

            // Should show recovery codes or success confirmation
            await page.WaitForURLAsync(url =>
                url.Contains("ShowRecoveryCodes") || url.Contains("EnableAuthenticator") || url.Contains("TwoFactorAuthentication"));
            var bodyText = await page.TextContentAsync("body");
            Assert.Contains("verified", bodyText, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TwoFactor_Login_WithRecoveryCode_Succeeds()
    {
        var (email, password) = await CreateAndLoginAsync();
        string recoveryCode;

        // Setup TOTP and capture a recovery code
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
            var sharedKey = (await sharedKeyEl.First.TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
            var keyBytes = Base32Encoding.ToBytes(sharedKey);
            var totp = new Totp(keyBytes);
            var code = totp.ComputeTotp();

            await setupPage.FillAsync("input[name='Input.Code']", code);
            await setupPage.ClickAsync("button.btn-primary");

            // Generate recovery codes
            await setupPage.GotoAsync("/Account/Manage/GenerateRecoveryCodes");
            await setupPage.ClickAsync("button.btn-danger");
            await setupPage.WaitForURLAsync("**/Account/Manage/ShowRecoveryCodes**");

            var codeEl = setupPage.Locator("code").First;
            recoveryCode = (await codeEl.TextContentAsync() ?? string.Empty).Trim();
        }

        // Now login with recovery code
        var (loginCtx, loginPage) = await fixture.NewPageAsync();
        await using (loginCtx)
        {
            await loginPage.GotoAsync("/Account/Login");
            await loginPage.FillAsync("input[name='Input.Email']", email);
            await loginPage.FillAsync("input[name='Input.Password']", password);
            await loginPage.ClickAsync("button[type='submit']");

            // Should be challenged for 2FA
            await loginPage.WaitForURLAsync("**/Account/LoginWith2fa**");

            // Use recovery code path
            await loginPage.ClickAsync("a[href*='LoginWithRecoveryCode']");
            await loginPage.WaitForURLAsync("**/Account/LoginWithRecoveryCode**");
            await loginPage.FillAsync("input[name='Input.RecoveryCode']", recoveryCode);
            await loginPage.ClickAsync("button[type='submit']");

            await loginPage.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", loginPage.Url);
        }
    }

    private async Task<(string Email, string Password)> CreateAndLoginAsync()
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
        }

        return (email, password);
    }
}