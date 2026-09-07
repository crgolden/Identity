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
            await page.ClickAsync(PasskeySelectors.Register);

            await page.WaitForURLAsync(
                url => !url.Contains("/Account/Manage/Passkeys", StringComparison.OrdinalIgnoreCase),
                new PageWaitForURLOptions { Timeout = CeremonyTimeoutMs });

            var status = page.Locator(StatusMessageSelector);
            var reported = await status.CountAsync() > 0 ? await status.InnerTextAsync() : string.Empty;
            Assert.False(
                reported.Contains("Could not add", StringComparison.OrdinalIgnoreCase),
                $"Identity refused the attestation, so the credential never serialized correctly: {reported}");

            var credentials = await page.Context.Credentials.GetAsync();
            Assert.True(
                credentials.Count == 1,
                $"The Add passkey button should mint exactly one credential; the authenticator holds {credentials.Count}.");

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
