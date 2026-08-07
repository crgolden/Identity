namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
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
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await page.ClickAsync("#enable-authenticator");

            await Assertions.Expect(page.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            var sharedKeyEl = page.Locator("#shared-key");
            var sharedKey = (await sharedKeyEl.First.TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            var keyBytes = Base32Encoding.ToBytes(sharedKey);
            var totp = new Totp(keyBytes);
            var code = totp.ComputeTotp();

            await page.FillAsync("input[name='Input.Code']", code);
            await page.ClickAsync("#verify-authenticator-submit");

            await Assertions.Expect(page.GetByText("verified", new() { Exact = false })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        }
    }

    [Fact]
    public async Task TwoFactor_Login_WithRecoveryCode_Succeeds()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();
        string recoveryCode;

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
            var sharedKey = (await sharedKeyEl.First.TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
            var keyBytes = Base32Encoding.ToBytes(sharedKey);
            var totp = new Totp(keyBytes);
            var code = totp.ComputeTotp();

            await setupPage.FillAsync("input[name='Input.Code']", code);
            await setupPage.ClickAsync("#verify-authenticator-submit");

            await setupPage.GotoAsync("/Account/Manage/GenerateRecoveryCodes");
            await setupPage.ClickAsync("#generate-codes-submit");

            await Assertions.Expect(setupPage.Locator("#recovery-code-0")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            var codeEl = setupPage.Locator("#recovery-code-0");
            recoveryCode = (await codeEl.TextContentAsync() ?? string.Empty).Trim();
        }

        var (loginCtx, loginPage) = await fixture.NewPageAsync();
        await using (loginCtx)
        {
            await loginPage.GotoAsync("/Account/Login");
            await loginPage.FillAsync("input[name='Input.Email']", email);
            await loginPage.FillAsync("input[name='Input.Password']", password);
            await loginPage.ClickAsync("#login-submit");

            await Assertions.Expect(loginPage).ToHaveURLAsync(new Regex("/Account/LoginWith2fa"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await loginPage.ClickAsync("#recovery-code-login");
            await Assertions.Expect(loginPage).ToHaveURLAsync(new Regex("/Account/LoginWithRecoveryCode"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await loginPage.FillAsync("input[name='Input.RecoveryCode']", recoveryCode);
            await loginPage.ClickAsync("#recovery-code-submit");

            await Assertions.Expect(loginPage).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await page.ClickAsync("#enable-authenticator");
            await Assertions.Expect(page.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            var sharedKey = (await page.Locator("#shared-key").TextContentAsync() ?? string.Empty)
                .Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
            var totp = new Totp(Base32Encoding.ToBytes(sharedKey));
            await page.FillAsync("input[name='Input.Code']", totp.ComputeTotp());
            await page.ClickAsync("#verify-authenticator-submit");

            await Assertions.Expect(page.GetByText("verified", new() { Exact = false })).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
            await page.ClickAsync("#reset-authenticator");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Manage/ResetAuthenticator"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#reset-authenticator-button");

            await Assertions.Expect(page.Locator("#shared-key")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
            Assert.Contains("EnableAuthenticator", page.Url);
        }
    }
}