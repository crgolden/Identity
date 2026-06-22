namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;
using OtpNet;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class Disable2faTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Disable2fa_AfterSetup_SubsequentLogin_DoesNotRequire2fa()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        // Capture the shared key outside the setup context so it can be reused in the disable step.
        var capturedSharedKey = string.Empty;

        // --- Setup TOTP ---
        var (setupCtx, setupPage) = await fixture.NewPageAsync();
        await using (setupCtx)
        {
            await setupPage.GotoAsync("/Account/Login");
            await setupPage.FillAsync("input[name='Input.Email']", email);
            await setupPage.FillAsync("input[name='Input.Password']", password);
            await setupPage.ClickAsync("#login-submit");
            await Assertions.Expect(setupPage).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await setupPage.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await setupPage.ClickAsync("#enable-authenticator");
            await Assertions.Expect(setupPage.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            var sharedKeyEl = setupPage.Locator("#shared-key");
            capturedSharedKey = (await sharedKeyEl.TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            var keyBytes = Base32Encoding.ToBytes(capturedSharedKey);
            var totp = new Totp(keyBytes);
            var code = totp.ComputeTotp();

            await setupPage.FillAsync("input[name='Input.Code']", code);
            await setupPage.ClickAsync("#verify-authenticator-submit");

            await Assertions.Expect(setupPage).ToHaveURLAsync(new Regex("ShowRecoveryCodes|TwoFactorAuthentication"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        }

        // --- Verify 2FA is required at login ---
        var (loginCtx, loginPage) = await fixture.NewPageAsync();
        await using (loginCtx)
        {
            await loginPage.GotoAsync("/Account/Login");
            await loginPage.FillAsync("input[name='Input.Email']", email);
            await loginPage.FillAsync("input[name='Input.Password']", password);
            await loginPage.ClickAsync("#login-submit");
            await Assertions.Expect(loginPage).ToHaveURLAsync(new Regex("/Account/LoginWith2fa"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        }

        // --- Disable 2FA ---
        var (disableCtx, disablePage) = await fixture.NewPageAsync();
        await using (disableCtx)
        {
            // Login with password — 2FA is now required
            await disablePage.GotoAsync("/Account/Login");
            await disablePage.FillAsync("input[name='Input.Email']", email);
            await disablePage.FillAsync("input[name='Input.Password']", password);
            await disablePage.ClickAsync("#login-submit");
            await Assertions.Expect(disablePage).ToHaveURLAsync(new Regex("/Account/LoginWith2fa"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Complete 2FA using the shared key captured during setup
            var disableKeyBytes = Base32Encoding.ToBytes(capturedSharedKey);
            var disableTotp = new Totp(disableKeyBytes);
            var disableCode = disableTotp.ComputeTotp();
            await disablePage.FillAsync("input[name='Input.TwoFactorCode']", disableCode);
            await disablePage.ClickAsync("#login-2fa-submit");
            await Assertions.Expect(disablePage).Not.ToHaveURLAsync(new Regex("/Account/LoginWith2fa"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Navigate to Disable2fa and confirm
            await disablePage.GotoAsync("/Account/Manage/Disable2fa");
            await disablePage.WaitForURLAsync("**/Account/Manage/Disable2fa**");
            await disablePage.ClickAsync("#disable-2fa-submit");
            await Assertions.Expect(disablePage).ToHaveURLAsync(new Regex("/Account/Manage/TwoFactorAuthentication"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        }

        // --- Verify 2FA is no longer required ---
        var (verifyCtx, verifyPage) = await fixture.NewPageAsync();
        await using (verifyCtx)
        {
            await verifyPage.GotoAsync("/Account/Login");
            await verifyPage.FillAsync("input[name='Input.Email']", email);
            await verifyPage.FillAsync("input[name='Input.Password']", password);
            await verifyPage.ClickAsync("#login-submit");

            // Should land somewhere other than the 2FA challenge page
            await Assertions.Expect(verifyPage).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("LoginWith2fa", verifyPage.Url);
        }
    }
}
