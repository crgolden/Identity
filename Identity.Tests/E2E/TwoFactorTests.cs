namespace Identity.Tests.E2E;

using Infrastructure;
using Microsoft.Playwright;
using OtpNet;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class TwoFactorAuthenticationTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task TwoFactor_Setup_Login_WithTotpCode_Succeeds()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login first
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Navigate to 2FA setup
            await page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await page.ClickAsync("#enable-authenticator");

            // WaitForURLAsync misses navigations that complete before the listener registers; poll DOM instead.
            await Assertions.Expect(page.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            // Extract shared key from page
            var sharedKeyEl = page.Locator("#shared-key");
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
            await page.ClickAsync("#verify-authenticator-submit");

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
        var (email, password) = await fixture.CreateConfirmedUserAsync();
        string recoveryCode;

        // Setup TOTP and capture a recovery code
        var (setupCtx, setupPage) = await fixture.NewPageAsync();
        await using (setupCtx)
        {
            await setupPage.GotoAsync("/Account/Login");
            await setupPage.FillAsync("input[name='Input.Email']", email);
            await setupPage.FillAsync("input[name='Input.Password']", password);
            await setupPage.ClickAsync("#login-submit");
            await setupPage.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            await setupPage.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await setupPage.ClickAsync("#enable-authenticator");
            await Assertions.Expect(setupPage.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            var sharedKeyEl = setupPage.Locator("#shared-key");
            var sharedKey = (await sharedKeyEl.First.TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
            var keyBytes = Base32Encoding.ToBytes(sharedKey);
            var totp = new Totp(keyBytes);
            var code = totp.ComputeTotp();

            await setupPage.FillAsync("input[name='Input.Code']", code);
            await setupPage.ClickAsync("#verify-authenticator-submit");

            // Generate recovery codes
            await setupPage.GotoAsync("/Account/Manage/GenerateRecoveryCodes");
            await setupPage.ClickAsync("#generate-codes-submit");
            await setupPage.WaitForURLAsync("**/Account/Manage/ShowRecoveryCodes**");

            var codeEl = setupPage.Locator("#recovery-code-0");
            recoveryCode = (await codeEl.TextContentAsync() ?? string.Empty).Trim();
        }

        // Now login with recovery code
        var (loginCtx, loginPage) = await fixture.NewPageAsync();
        await using (loginCtx)
        {
            await loginPage.GotoAsync("/Account/Login");
            await loginPage.FillAsync("input[name='Input.Email']", email);
            await loginPage.FillAsync("input[name='Input.Password']", password);
            await loginPage.ClickAsync("#login-submit");

            // Should be challenged for 2FA
            await loginPage.WaitForURLAsync("**/Account/LoginWith2fa**");

            // Use recovery code path
            await loginPage.ClickAsync("#recovery-code-login");
            await loginPage.WaitForURLAsync("**/Account/LoginWithRecoveryCode**");
            await loginPage.FillAsync("input[name='Input.RecoveryCode']", recoveryCode);
            await loginPage.ClickAsync("#recovery-code-submit");

            await loginPage.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", loginPage.Url);
        }
    }

    [Fact]
    public async Task TwoFactor_ResetAuthenticator_DisablesAndRedirectsToSetup()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Enable 2FA
            await page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await page.ClickAsync("#enable-authenticator");
            await Assertions.Expect(page.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            var sharedKey = (await page.Locator("#shared-key").TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
            var totp = new Totp(Base32Encoding.ToBytes(sharedKey));
            await page.FillAsync("input[name='Input.Code']", totp.ComputeTotp());
            await page.ClickAsync("#verify-authenticator-submit");
            await page.WaitForURLAsync(url =>
                url.Contains("ShowRecoveryCodes") || url.Contains("TwoFactorAuthentication"));

            // Reset authenticator key
            await page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await page.ClickAsync("#reset-authenticator");
            await page.WaitForURLAsync("**/Account/Manage/ResetAuthenticator**");
            await page.ClickAsync("#reset-authenticator-button");

            // Should redirect to EnableAuthenticator to set up a new key
            await Assertions.Expect(page.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
            Assert.Contains("EnableAuthenticator", page.Url);
        }
    }
}
