namespace Identity.Tests.E2E.Infrastructure;

using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class NewSessionSteps(BrowserScenario scenario, Member member)
{
    [Then("they can sign in with their current details in a new session")]
    public async Task ThenTheyCanSignInWithTheirCurrentDetailsInANewSession()
    {
        var page = await SubmitInNewSessionAsync(member.Email, member.Password);
        await Assertions.Expect(page).Not.ToHaveURLAsync(UrlPatterns.Login());
        Assert.DoesNotContain(PageRoutes.Login, page.Url, StringComparison.Ordinal);
    }

    [Then("signing in to their account in a new session is refused")]
    public Task ThenSigningInToTheirAccountInANewSessionIsRefused() => AssertRefusedAsync(member.Email, member.Password);

    [Then("their previous password is refused in a new session")]
    public Task ThenTheirPreviousPasswordIsRefusedInANewSession() => AssertRefusedAsync(member.Email, member.PreviousPassword);

    [Then("their previous email is refused in a new session")]
    public Task ThenTheirPreviousEmailIsRefusedInANewSession() => AssertRefusedAsync(member.PreviousEmail, member.Password);

    private async Task AssertRefusedAsync(string email, string password)
    {
        var page = await SubmitInNewSessionAsync(email, password);
        await Assertions.Expect(page).ToHaveURLAsync(UrlPatterns.Login());
        Assert.NotNull(await page.TextContentAsync("#validation-errors"));
    }

    private async Task<IPage> SubmitInNewSessionAsync(string email, string password)
    {
        var page = await scenario.OpenAnotherPageAsync();
        await page.GotoAsync(PageRoutes.Login);
        await Member.SubmitCredentialsAsync(page, email, password);
        return page;
    }
}
