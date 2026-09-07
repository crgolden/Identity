namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;
using Synthetic;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class PasskeyCeremonyTests(PlaywrightFixture fixture)
{
    private const string StatusMessageSelector = "#status-message";
    private const float CeremonyTimeoutMs = 30_000;
    private const float AutofillGraceMs = 5_000;

    [Fact]
    public async Task RegisterPasskey_ThenSignInWithIt_Succeeds()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.Context.Credentials.InstallAsync();
            await CredentialSerialization.InstallAsync(page.Context);

            await SignInWithPasswordAsync(page, email, password);

            await page.GotoAsync("/Account/Manage/Passkeys");

            var ceremonyPrerequisites = await page.EvaluateAsync<string>(
                @"() => {
                    const element = document.querySelector('passkey-submit');
                    const button = document.querySelector('button[name=""__passkeySubmit""]');
                    return JSON.stringify({
                        isSecureContext: window.isSecureContext,
                        hasCredentials: navigator.credentials !== undefined,
                        hasPublicKeyCredential: typeof PublicKeyCredential !== 'undefined',
                        hasParseCreationOptions: typeof PublicKeyCredential !== 'undefined'
                            && typeof PublicKeyCredential.parseCreationOptionsFromJSON === 'function',
                        hasParseRequestOptions: typeof PublicKeyCredential !== 'undefined'
                            && typeof PublicKeyCredential.parseRequestOptionsFromJSON === 'function',
                        customElementDefined: customElements.get('passkey-submit') !== undefined,
                        elementUpgraded: element instanceof (customElements.get('passkey-submit') ?? HTMLElement)
                            && element?.internals !== undefined,
                        elementSeesForm: element?.internals?.form !== null
                            && element?.internals?.form !== undefined,
                        operation: element?.getAttribute('operation'),
                        clickTargetIsSubmitter: button?.id === 'add-passkey',
                    });
                }");
            Assert.False(
                ceremonyPrerequisites.Contains("false", StringComparison.Ordinal)
                    || ceremonyPrerequisites.Contains("null", StringComparison.Ordinal),
                $"The page cannot start a WebAuthn ceremony, so passkey-submit.js posts the form without a credential: {ceremonyPrerequisites}");

            await page.RunAndWaitForResponseAsync(
                () => page.ClickAsync(PasskeySelectors.Register),
                response => response.Request.Method == "POST"
                            && response.Url.Contains("/Account/Manage/Passkeys", StringComparison.OrdinalIgnoreCase),
                new PageRunAndWaitForResponseOptions { Timeout = CeremonyTimeoutMs });
            await page.WaitForLoadStateAsync();

            var status = page.Locator(StatusMessageSelector);
            await status.WaitForAsync(new LocatorWaitForOptions { Timeout = CeremonyTimeoutMs });
            var reported = await status.InnerTextAsync();
            Assert.False(
                reported.Contains("Could not add", StringComparison.OrdinalIgnoreCase),
                $"Identity refused the attestation, so the credential never serialized correctly: {reported}");

            var credentials = await page.Context.Credentials.GetAsync();
            var mintFailure = $"The Add passkey button should mint exactly one credential; the authenticator holds {credentials.Count}. "
                              + $"Identity said: '{reported.Trim()}'. Ceremony prerequisites: {ceremonyPrerequisites}";
            Assert.True(credentials.Count == 1, mintFailure);

            await SyntheticAccount.SignOutAsync(page);
            await SignInWithPasskeyAsync(page, email);

            await Assertions.Expect(page).Not.ToHaveURLAsync(
                new Regex("/Account/Login"),
                new PageAssertionsToHaveURLOptions { Timeout = CeremonyTimeoutMs });
        }
    }

    private static async Task SignInWithPasskeyAsync(IPage page, string email)
    {
        await page.GotoAsync("/Account/Login?ReturnUrl=%2F");
        if (await ConditionalMediationAlreadySignedInAsync(page))
        {
            return;
        }

        try
        {
            await page.FillAsync("input[name='Input.Email']", email);
            await page.ClickAsync(PasskeySelectors.SignIn, new PageClickOptions { Timeout = CeremonyTimeoutMs });
        }
        catch (PlaywrightException) when (!IsOnLoginPage(page))
        {
            await page.WaitForLoadStateAsync();
        }
    }

    private static async Task<bool> ConditionalMediationAlreadySignedInAsync(IPage page)
    {
        try
        {
            await page.Locator("input[name='Input.Email']").WaitForAsync(
                new LocatorWaitForOptions { Timeout = AutofillGraceMs });
        }
        catch (TimeoutException)
        {
            return !IsOnLoginPage(page);
        }

        return !IsOnLoginPage(page);
    }

    private static bool IsOnLoginPage(IPage page) =>
        page.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase);

    private static async Task SignInWithPasswordAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/Account/Login");
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(
            new Regex("/Account/Login"),
            new PageAssertionsToHaveURLOptions { Timeout = CeremonyTimeoutMs });
    }
}
