namespace Identity.Tests.E2E.PasswordReset;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class PasswordResetSteps(BrowserScenario scenario, Member member)
{
    [When("they reset their password through the emailed link")]
    public async Task WhenTheyResetTheirPasswordThroughTheEmailedLink()
    {
        var newPassword = Generated.NewPassword();
        await scenario.Page.GotoAsync(PageRoutes.ForgotPassword);
        await scenario.Page.FillAsync("input[name='Input.Email']", member.Email);
        await scenario.Page.ClickAsync("#forgot-password-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.ForgotPasswordConfirmation());

        var resetLink = scenario.Fixture.ExtractEmailLink(scenario.Fixture.Email.TakeEmail(member.Email));
        await scenario.Page.GotoAsync(resetLink);
        await scenario.Page.WaitForURLAsync("**/Account/ResetPassword**");
        await scenario.Page.FillAsync("input[name='Input.Password']", newPassword);
        await scenario.Page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
        await scenario.Page.ClickAsync("#reset-password-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.ResetPasswordConfirmation());
        member.ChangePassword(newPassword);
    }
}
