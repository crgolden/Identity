namespace Identity.Tests.E2E.TwoFactor;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using OtpNet;
using Reqnroll;

[Binding]
public sealed class TwoFactorSteps(BrowserScenario scenario, Member member)
{
    private string? _sharedKey;
    private string? _recoveryCode;
    private IPage? _newSessionPage;

    private string SharedKey => _sharedKey ?? throw new InvalidOperationException("No authenticator was set up in this scenario.");

    private string RecoveryCode => _recoveryCode ?? throw new InvalidOperationException("No recovery codes were generated in this scenario.");

    private IPage NewSessionPage => _newSessionPage ?? throw new InvalidOperationException("No new session was opened in this scenario.");

    [When("they set up an authenticator app")]
    [Given("they have set up an authenticator app")]
    public async Task SetUpAnAuthenticatorApp()
    {
        await scenario.Page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
        await scenario.Page.ClickAsync("#enable-authenticator");
        await Assertions.Expect(scenario.Page.Locator("#shared-key")).ToBeVisibleAsync();
        var sharedKeyText = await scenario.Page.Locator("#shared-key").TextContentAsync();
        Assert.NotNull(sharedKeyText);
        _sharedKey = string.Concat(sharedKeyText.Where(char.IsLetterOrDigit)).ToUpperInvariant();
        await scenario.Page.FillAsync("input[name='Input.Code']", CurrentCode());
        await scenario.Page.ClickAsync("#verify-authenticator-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.ShowRecoveryCodesOrTwoFactorAuthentication());
    }

    [Given("they have generated recovery codes")]
    public async Task GivenTheyHaveGeneratedRecoveryCodes()
    {
        await scenario.Page.GotoAsync("/Account/Manage/GenerateRecoveryCodes");
        await scenario.Page.ClickAsync("#generate-codes-submit");
        await Assertions.Expect(scenario.Page.Locator("#recovery-code-0")).ToBeVisibleAsync();
        var recoveryCodeText = await scenario.Page.Locator("#recovery-code-0").TextContentAsync();
        Assert.NotNull(recoveryCodeText);
        _recoveryCode = recoveryCodeText.Trim();
    }

    [Given("signing in again in a new session asks for a code")]
    public async Task GivenSigningInAgainInANewSessionAsksForACode()
    {
        var page = await SubmitPasswordInNewSessionAsync();
        await Assertions.Expect(page).ToHaveURLAsync(UrlPatterns.LoginWith2fa());
    }

    [When("they sign in with a recovery code in a new session")]
    public async Task WhenTheySignInWithARecoveryCodeInANewSession()
    {
        _newSessionPage = await SubmitPasswordInNewSessionAsync();
        await Assertions.Expect(NewSessionPage).ToHaveURLAsync(UrlPatterns.LoginWith2fa());
        await NewSessionPage.ClickAsync("#recovery-code-login");
        await Assertions.Expect(NewSessionPage).ToHaveURLAsync(UrlPatterns.LoginWithRecoveryCode());
        await NewSessionPage.FillAsync("input[name='Input.RecoveryCode']", RecoveryCode);
        await NewSessionPage.ClickAsync("#recovery-code-submit");
    }

    [When("they reset their authenticator")]
    public async Task WhenTheyResetTheirAuthenticator()
    {
        await scenario.Page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
        await scenario.Page.ClickAsync("#reset-authenticator");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.ManageResetAuthenticator());
        await scenario.Page.ClickAsync("#reset-authenticator-button");
    }

    [When("they sign in with a code in a new session and turn two-factor sign-in off")]
    public async Task WhenTheySignInWithACodeInANewSessionAndTurnTwoFactorSignInOff()
    {
        var page = await SubmitPasswordInNewSessionAsync();
        await Assertions.Expect(page).ToHaveURLAsync(UrlPatterns.LoginWith2fa());
        await page.FillAsync("input[name='Input.TwoFactorCode']", CurrentCode());
        await page.ClickAsync("#login-2fa-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(UrlPatterns.LoginWith2fa());

        await page.GotoAsync("/Account/Manage/Disable2fa");
        await page.WaitForURLAsync("**/Account/Manage/Disable2fa**");
        await page.ClickAsync("#disable-2fa-submit");
        await Assertions.Expect(page).ToHaveURLAsync(UrlPatterns.ManageTwoFactorAuthentication());
    }

    [Then("two-factor sign-in is on for their account")]
    public async Task ThenTwoFactorSignInIsOnForTheirAccount()
    {
        await scenario.Page.GotoAsync("/Account/Manage/TwoFactorAuthentication");
        await Assertions.Expect(scenario.Page.Locator("#reset-authenticator")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#disable-2fa-link")).ToBeVisibleAsync();
    }

    [Then("the new session is signed in")]
    public async Task ThenTheNewSessionIsSignedIn()
    {
        await Assertions.Expect(NewSessionPage).Not.ToHaveURLAsync(UrlPatterns.Login());
        Assert.DoesNotContain(PageRoutes.Login, NewSessionPage.Url, StringComparison.Ordinal);
    }

    [Then("they are taken back to set up an authenticator")]
    public async Task ThenTheyAreTakenBackToSetUpAnAuthenticator()
    {
        await Assertions.Expect(scenario.Page.Locator("#shared-key")).ToBeVisibleAsync();
        Assert.Contains("EnableAuthenticator", scenario.Page.Url, StringComparison.Ordinal);
    }

    [Then("signing in again in a new session asks for no code")]
    public async Task ThenSigningInAgainInANewSessionAsksForNoCode()
    {
        var page = await SubmitPasswordInNewSessionAsync();
        await Assertions.Expect(page).Not.ToHaveURLAsync(UrlPatterns.Login());
        Assert.DoesNotContain("LoginWith2fa", page.Url, StringComparison.Ordinal);
    }

    private string CurrentCode() => new Totp(Base32Encoding.ToBytes(SharedKey)).ComputeTotp();

    private async Task<IPage> SubmitPasswordInNewSessionAsync()
    {
        var page = await scenario.OpenAnotherPageAsync();
        await page.GotoAsync(PageRoutes.Login);
        await member.SubmitCredentialsAsync(page, member.Password);
        return page;
    }
}
