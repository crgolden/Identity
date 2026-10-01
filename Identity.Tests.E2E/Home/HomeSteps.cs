namespace Identity.Tests.E2E.Home;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class HomeSteps(BrowserScenario scenario, Member member)
{
    [When("a visitor opens the home page")]
    [When("they open the home page")]
    public async Task WhenTheyOpenTheHomePage()
    {
        await scenario.Page.GotoAsync("/");
    }

    [Then("they are offered to create an account or sign in")]
    public async Task ThenTheyAreOfferedToCreateAnAccountOrSignIn()
    {
        await Assertions.Expect(scenario.Page.Locator("#create-account")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#sign-in")).ToBeVisibleAsync();
    }

    [Then("they are not offered account links")]
    public async Task ThenTheyAreNotOfferedAccountLinks()
    {
        await Assertions.Expect(scenario.Page.Locator("#manage-account")).Not.ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#review-grants")).Not.ToBeVisibleAsync();
    }

    [Then("they are offered their account and the applications they allowed")]
    public async Task ThenTheyAreOfferedTheirAccountAndTheApplicationsTheyAllowed()
    {
        await Assertions.Expect(scenario.Page.Locator("#manage-account")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#review-grants")).ToBeVisibleAsync();
    }

    [Then("they are not offered to create an account or sign in")]
    public async Task ThenTheyAreNotOfferedToCreateAnAccountOrSignIn()
    {
        await Assertions.Expect(scenario.Page.Locator("#create-account")).Not.ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#sign-in")).Not.ToBeVisibleAsync();
    }

    [Then("the page names the member who is signed in")]
    public async Task ThenThePageNamesTheMemberWhoIsSignedIn()
    {
        var memberId = await member.GetIdAsync();
        await Assertions.Expect(scenario.Page.Locator($"#signed-in-user-{memberId}")).ToBeVisibleAsync();
    }

    [Then("they are offered the admin area")]
    public async Task ThenTheyAreOfferedTheAdminArea()
    {
        await Assertions.Expect(scenario.Page.Locator("#home-admin")).ToBeVisibleAsync();
    }

    [Then("they are not offered the admin area")]
    public async Task ThenTheyAreNotOfferedTheAdminArea()
    {
        await Assertions.Expect(scenario.Page.Locator("#home-admin")).Not.ToBeVisibleAsync();
    }
}
