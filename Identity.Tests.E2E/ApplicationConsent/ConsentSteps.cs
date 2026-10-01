namespace Identity.Tests.E2E.ApplicationConsent;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class ConsentSteps(BrowserScenario scenario)
{
    private string? _clientId;
    private IRequest? _redirect;

    private string ClientId => _clientId ?? throw new InvalidOperationException("No application was registered in this scenario.");

    private IRequest Redirect => _redirect ?? throw new InvalidOperationException("The application received no redirect in this scenario.");

    [Given("an application that asks for consent")]
    public async Task GivenAnApplicationThatAsksForConsent()
    {
        _clientId = await new TestClientHelper(scenario.Fixture).SeedConsentClientAsync();
    }

    [When("the application asks the member for access")]
    public async Task WhenTheApplicationAsksTheMemberForAccess()
    {
        await scenario.Page.GotoAsync(AuthorizeRequest.Url(ClientId, OidcStandardConstants.LoopbackRedirectUri, Generated.NewClaimValue()));
        Assert.Contains(PageRoutes.Consent, scenario.Page.Url, StringComparison.Ordinal);
    }

    [When("they allow every requested permission")]
    public async Task WhenTheyAllowEveryRequestedPermission()
    {
        foreach (var checkbox in await scenario.Page.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])"))
        {
            await checkbox.CheckAsync();
        }

        await SubmitAndCaptureRedirectAsync("#consent-allow");
    }

    [When("they decline the request")]
    public async Task WhenTheyDeclineTheRequest()
    {
        await SubmitAndCaptureRedirectAsync("#consent-deny");
    }

    [When("they allow none of the requested permissions")]
    public async Task WhenTheyAllowNoneOfTheRequestedPermissions()
    {
        foreach (var checkbox in await scenario.Page.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])"))
        {
            await checkbox.UncheckAsync();
        }

        await scenario.Page.RunAndWaitForResponseAsync(
            () => scenario.Page.ClickAsync("#consent-allow"),
            r => r.Url.Contains(PageRoutes.Consent, StringComparison.Ordinal) && string.Equals(r.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal));
    }

    [Then("the application receives an authorization code")]
    public void ThenTheApplicationReceivesAnAuthorizationCode()
    {
        Assert.Contains("code=", Redirect.Url, StringComparison.Ordinal);
    }

    [Then("the application is told access was denied")]
    public void ThenTheApplicationIsToldAccessWasDenied()
    {
        Assert.Contains("error=access_denied", Redirect.Url, StringComparison.Ordinal);
    }

    [Then("they are asked to choose at least one permission")]
    public async Task ThenTheyAreAskedToChooseAtLeastOnePermission()
    {
        Assert.Contains(PageRoutes.Consent, scenario.Page.Url, StringComparison.Ordinal);
        await Assertions.Expect(scenario.Page.Locator("#must-choose-one-error")).ToBeVisibleAsync();
    }

    private async Task SubmitAndCaptureRedirectAsync(string buttonId)
    {
        _redirect = await scenario.Page.RunAndWaitForRequestAsync(
            async () => await scenario.Page.ClickAsync(buttonId),
            r => r.Url.Contains(OidcStandardConstants.LoopbackHost, StringComparison.Ordinal));
    }
}
