namespace Identity.Tests.E2E.PasskeySignIn;

using Identity.Extensions;
using Identity.Tests.E2E.Infrastructure;
using Identity.Tests.E2E.Synthetic;
using Reqnroll;

[Binding]
public sealed class PasskeySignInSteps(BrowserScenario scenario, Member member)
{
    private const string CreationOptionsPath = PasskeyEndpoints.AccountGroupPrefix + PasskeyEndpoints.CreationOptionsRoute;

    [Given("a member signed in on a browser that can hold a passkey")]
    public async Task GivenAMemberSignedInOnABrowserThatCanHoldAPasskey()
    {
        await scenario.Page.Context.Credentials.InstallAsync();
        await CredentialSerialization.InstallAsync(scenario.Page.Context);
        await member.CreateAsync();
        await member.SignInAsync();
    }

    [When("they add a passkey to their account")]
    public async Task WhenTheyAddAPasskeyToTheirAccount()
    {
        await scenario.Page.GotoAsync("/Account/Manage/Passkeys");
        Assert.Empty(await scenario.Page.EvaluateAsync<string[]>(BrowserScripts.CeremonyPrerequisites));

        var creationOptionsRequests = new List<string>();
        scenario.Page.Request += (_, request) =>
        {
            if (request.Url.Contains(CreationOptionsPath, StringComparison.OrdinalIgnoreCase))
            {
                creationOptionsRequests.Add(request.Method);
            }
        };

        await scenario.Page.RunAndWaitForResponseAsync(
            () => scenario.Page.ClickAsync(PasskeySelectors.Register),
            response => string.Equals(response.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal)
                        && response.Url.Contains("/Account/Manage/Passkeys", StringComparison.OrdinalIgnoreCase));
        await scenario.Page.WaitForLoadStateAsync();
        await scenario.Page.Locator("#status-message").WaitForAsync();

        Assert.Single(creationOptionsRequests);
        Assert.Single(await scenario.Page.Context.Credentials.GetAsync());
    }

    [When("they sign out and sign in again with the passkey")]
    public async Task WhenTheySignOutAndSignInAgainWithThePasskey()
    {
        await SyntheticAccount.SignOutAsync(scenario.Page);
        await scenario.Page.Context.AddInitScriptAsync(BrowserScripts.DisableConditionalMediation);
        await scenario.Page.GotoAsync($"{PageRoutes.Login}{SyntheticAccountConstants.ReturnToRootQuery}");
        await scenario.Page.FillAsync("input[name='Input.Email']", member.Email);
        await scenario.Page.ClickAsync(PasskeySelectors.SignIn);
    }
}
