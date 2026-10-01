namespace Identity.Tests.E2E.AdminGrants;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AdminGrantsSteps(BrowserScenario scenario)
{
    private string? _grantKey;

    private string GrantKey => _grantKey ?? throw new InvalidOperationException("No grant was issued in this scenario.");

    [Given("a member has approved an application")]
    public async Task GivenAMemberHasApprovedAnApplication()
    {
        var clientId = await new TestClientHelper(scenario.Fixture).SeedConsentClientAsync();
        var (email, password) = await scenario.Fixture.CreateConfirmedUserAsync();
        var memberPage = await scenario.OpenAnotherPageAsync();
        var approvingMember = new Member(scenario);
        approvingMember.Remember(email, password);
        await approvingMember.SignInAsync(memberPage);

        await memberPage.GotoAsync(AuthorizeRequest.Url(clientId, OidcStandardConstants.LoopbackRedirectUri, Generated.NewClaimValue()));
        Assert.Contains(PageRoutes.Consent, memberPage.Url, StringComparison.Ordinal);
        foreach (var checkbox in await memberPage.QuerySelectorAllAsync("input[id^='scope_']:not([disabled])"))
        {
            await checkbox.CheckAsync();
        }

        await memberPage.RunAndWaitForRequestAsync(
            async () => await memberPage.ClickAsync("#consent-allow"),
            r => r.Url.Contains(OidcStandardConstants.LoopbackHost, StringComparison.Ordinal));
        _grantKey = await scenario.Fixture.GetPersistedGrantKeyAsync(clientId);
    }

    [When("they open that grant from the grant list")]
    public async Task WhenTheyOpenThatGrantFromTheGrantList()
    {
        await scenario.Page.GotoAsync("/Admin/PersistedGrants");
        await scenario.Page.ClickAsync($"[id='details-{GrantKey}']");
    }

    [When("they delete the grant")]
    public async Task WhenTheyDeleteTheGrant()
    {
        await scenario.Page.ClickAsync("#btn-delete");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.DeletePage());
        await scenario.Page.ClickAsync("#delete-submit");
    }

    [Then("they see the grant's details")]
    public async Task ThenTheySeeTheGrantsDetails()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminPersistedGrantsDetails());
        await Assertions.Expect(scenario.Page.Locator("#grant-client-id")).ToBeVisibleAsync();
    }

    [Then("the grant is no longer listed")]
    public async Task ThenTheGrantIsNoLongerListed()
    {
        await Assertions.Expect(scenario.Page).Not.ToHaveURLAsync(UrlPatterns.DeleteAnywhere());
        await scenario.Page.GotoAsync("/Admin/PersistedGrants");
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator($"[id='details-{GrantKey}']")).ToHaveCountAsync(0);
    }
}
