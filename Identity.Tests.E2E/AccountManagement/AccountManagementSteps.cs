namespace Identity.Tests.E2E.AccountManagement;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AccountManagementSteps(BrowserScenario scenario, Member member)
{
    [When("they change their password")]
    public async Task WhenTheyChangeTheirPassword()
    {
        var newPassword = Generated.NewPassword();
        await scenario.Page.GotoAsync("/Account/Manage/ChangePassword");
        await scenario.Page.FillAsync("input[name='Input.OldPassword']", member.Password);
        await scenario.Page.FillAsync("input[name='Input.NewPassword']", newPassword);
        await scenario.Page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
        await scenario.Page.ClickAsync("#change-password-submit");
        await Assertions.Expect(scenario.Page.Locator("#password-changed")).ToBeVisibleAsync();
        member.ChangePassword(newPassword);
    }

    [When("they delete their account")]
    public async Task WhenTheyDeleteTheirAccount()
    {
        await scenario.Page.GotoAsync("/Account/Manage/DeletePersonalData");
        await scenario.Page.FillAsync("input[name='Input.Password']", member.Password);
        await scenario.Page.ClickAsync("#delete-account-submit");
        await Assertions.Expect(scenario.Page).Not.ToHaveURLAsync(UrlPatterns.Manage());
    }

    [When("they change their email and confirm the new address")]
    public async Task WhenTheyChangeTheirEmailAndConfirmTheNewAddress()
    {
        var newEmail = $"e2e-new-{Guid.NewGuid()}@test.invalid";
        await scenario.Page.GotoAsync("/Account/Manage/Email");
        await scenario.Page.FillAsync("input[name='Input.NewEmail']", newEmail);
        await scenario.Page.ClickAsync("#change-email-button");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.ManageEmail());
        await Assertions.Expect(scenario.Page.Locator("#email-change-link-sent")).ToBeVisibleAsync();

        var confirmLink = scenario.Fixture.ExtractEmailLink(scenario.Fixture.Email.TakeEmail(newEmail));
        await scenario.Page.GotoAsync(confirmLink);
        await scenario.Page.WaitForURLAsync("**/Account/ConfirmEmailChange**");
        member.ChangeEmail(newEmail);
    }

    [When("they change their email to the one already on the account")]
    public async Task WhenTheyChangeTheirEmailToTheOneAlreadyOnTheAccount()
    {
        await scenario.Page.GotoAsync("/Account/Manage/Email");
        await scenario.Page.FillAsync("input[name='Input.NewEmail']", member.Email);
        await scenario.Page.ClickAsync("#change-email-button");
    }

    [When("they open their session diagnostics")]
    public async Task WhenTheyOpenTheirSessionDiagnostics()
    {
        await scenario.Page.GotoAsync("/Account/Manage/Diagnostics", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    [Then("they are told the email is unchanged")]
    public async Task ThenTheyAreToldTheEmailIsUnchanged()
    {
        await Assertions.Expect(scenario.Page.Locator("#email-unchanged")).ToBeVisibleAsync();
    }

    [Then("no confirmation email is sent")]
    public void ThenNoConfirmationEmailIsSent()
    {
        Assert.False(scenario.Fixture.Email.HasEmailFor(member.Email));
    }

    [Then("they see the claims their session carries")]
    public async Task ThenTheySeeTheClaimsTheirSessionCarries()
    {
        await Assertions.Expect(scenario.Page.Locator("#diagnostics-claims")).ToBeVisibleAsync();
        Assert.NotEqual(0, await scenario.Page.Locator("[id^='diagnostics-claim-']").CountAsync());
    }
}
