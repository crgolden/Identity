namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Extensions;
using Identity.Tests.E2E.Infrastructure;
using Identity.Tests.E2E.Synthetic;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class PasskeyCeremonyTests(PlaywrightFixture fixture)
{
    private const string StatusMessageSelector = "#status-message";
    private const string CreationOptionsPath =
        PasskeyEndpoints.AccountGroupPrefix + PasskeyEndpoints.CreationOptionsRoute;

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

            var unmetCeremonyPrerequisites = await page.EvaluateAsync<string[]>(BrowserScripts.CeremonyPrerequisites);
            Assert.Empty(unmetCeremonyPrerequisites);

            var creationOptionsRequests = new List<string>();
            page.Request += (_, request) =>
            {
                if (request.Url.Contains(CreationOptionsPath, StringComparison.OrdinalIgnoreCase))
                {
                    creationOptionsRequests.Add(request.Method);
                }
            };

            await page.RunAndWaitForResponseAsync(
                () => page.ClickAsync(PasskeySelectors.Register),
                response => string.Equals(response.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal)
                            && response.Url.Contains("/Account/Manage/Passkeys", StringComparison.OrdinalIgnoreCase));
            await page.WaitForLoadStateAsync();
            await page.Locator(StatusMessageSelector).WaitForAsync();

            Assert.Single(creationOptionsRequests);

            var credentials = await page.Context.Credentials.GetAsync();
            Assert.Single(credentials);

            await SyntheticAccount.SignOutAsync(page);
            await SignInWithPasskeyAsync(page, email);

            await Assertions.Expect(page).Not.ToHaveURLAsync(
                new Regex(PageRoutes.Login));
        }
    }

    private static async Task SignInWithPasskeyAsync(IPage page, string email)
    {
        await page.Context.AddInitScriptAsync(BrowserScripts.DisableConditionalMediation);
        await page.GotoAsync($"{PageRoutes.Login}{SyntheticAccountConstants.ReturnToRootQuery}");
        await page.FillAsync("input[name='Input.Email']", email);
        await page.ClickAsync(PasskeySelectors.SignIn);
    }

    private static async Task SignInWithPasswordAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(PageRoutes.Login);
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(
            new Regex(PageRoutes.Login));
    }
}
