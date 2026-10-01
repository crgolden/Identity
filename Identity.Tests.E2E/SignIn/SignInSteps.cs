namespace Identity.Tests.E2E.SignIn;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class SignInSteps(BrowserScenario scenario, Member member)
{
    private readonly List<string> _scriptErrors = [];
    private bool _postSent;

    [Given("a visitor on the sign-in page")]
    public async Task GivenAVisitorOnTheSignInPage()
    {
        scenario.Page.PageError += (_, error) => _scriptErrors.Add(error);
        await scenario.Page.GotoAsync(PageRoutes.Login);
        await scenario.Page.WaitForFunctionAsync(BrowserScripts.LoginFormValidatorAttached);
    }

    [When("they sign in with their password")]
    public async Task WhenTheySignInWithTheirPassword()
    {
        await scenario.Page.GotoAsync(PageRoutes.Login);
        await member.SubmitCredentialsAsync(scenario.Page, member.Password);
    }

    [When("they sign in with a wrong password")]
    public async Task WhenTheySignInWithAWrongPassword()
    {
        await scenario.Page.GotoAsync(PageRoutes.Login);
        await member.SubmitCredentialsAsync(scenario.Page, Generated.NewPassword());
    }

    [When("they submit the form without filling it in")]
    public async Task WhenTheySubmitTheFormWithoutFillingItIn()
    {
        scenario.Page.Request += (_, request) => _postSent |= string.Equals(request.Method, HttpMethod.Post.Method, StringComparison.Ordinal);
        await scenario.Page.ClickAsync("#login-submit");
    }

    [When("they enter a wrong password as many times as the lockout policy allows")]
    public async Task WhenTheyEnterAWrongPasswordAsManyTimesAsTheLockoutPolicyAllows()
    {
        var maxFailedAttempts = scenario.Fixture.Factory.Services
            .GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;
        await scenario.Page.GotoAsync(PageRoutes.Login);

        for (var attempt = 1; attempt <= maxFailedAttempts; attempt++)
        {
            var postResponse = scenario.Page.WaitForResponseAsync(
                res => string.Equals(res.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal) && res.Url.Contains(PageRoutes.Login, StringComparison.Ordinal));
            await member.SubmitCredentialsAsync(scenario.Page, Generated.NewPassword());
            await postResponse;

            if (attempt < maxFailedAttempts)
            {
                await scenario.Page.Locator("input[name='Input.Email']").WaitForAsync();
            }
            else
            {
                await scenario.Page.Locator("#lockout-heading").WaitForAsync();
            }
        }
    }

    [When("they sign out")]
    public async Task WhenTheySignOut()
    {
        await scenario.Page.GotoAsync(PageRoutes.Logout);
        await scenario.Page.ClickAsync("#logout-submit");
        await scenario.Page.WaitForLoadStateAsync();
    }

    [When("they sign in from a link that returns them to {word}")]
    public async Task WhenTheySignInFromALinkThatReturnsThemTo(string returnAddress)
    {
        await scenario.Page.GotoAsync($"/Account/Login?ReturnUrl={returnAddress}");
        await member.SubmitCredentialsAsync(scenario.Page, member.Password);
    }

    [When("they sign in from a link that returns them to their account page")]
    public async Task WhenTheySignInFromALinkThatReturnsThemToTheirAccountPage()
    {
        await scenario.Page.GotoAsync("/Account/Login?ReturnUrl=%2FAccount%2FManage");
        await member.SubmitCredentialsAsync(scenario.Page, member.Password);
    }

    [Then("opening their account sends them to sign in")]
    public async Task ThenOpeningTheirAccountSendsThemToSignIn()
    {
        await scenario.Page.GotoAsync("/Account/Manage/Index");
        await scenario.Page.WaitForURLAsync(url => url.Contains(PageRoutes.Login, StringComparison.Ordinal));
        Assert.Contains(PageRoutes.Login, scenario.Page.Url, StringComparison.Ordinal);
    }

    [Then("they are still on this site")]
    public void ThenTheyAreStillOnThisSite()
    {
        Assert.Equal(new Uri(scenario.Fixture.BaseAddress).Host, new Uri(scenario.Page.Url).Host);
    }

    [Then("they land on their account page")]
    public async Task ThenTheyLandOnTheirAccountPage()
    {
        await Assertions.Expect(scenario.Page.Locator("#profile-form")).ToBeVisibleAsync();
        Assert.DoesNotContain(PageRoutes.Login, scenario.Page.Url, StringComparison.Ordinal);
    }

    [Then("they are signed in")]
    public async Task ThenTheyAreSignedIn()
    {
        await Assertions.Expect(scenario.Page).Not.ToHaveURLAsync(UrlPatterns.Login());
        Assert.DoesNotContain(PageRoutes.Login, scenario.Page.Url, StringComparison.Ordinal);
    }

    [Then("they stay on the sign-in page with the reason shown")]
    public async Task ThenTheyStayOnTheSignInPageWithTheReasonShown()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.Login());
        Assert.NotNull(await scenario.Page.TextContentAsync("#validation-errors"));
    }

    [Then("the email field asks for a value")]
    public async Task ThenTheEmailFieldAsksForAValue()
    {
        await Assertions.Expect(scenario.Page.Locator("#login-email-validation")).ToHaveClassAsync(new Regex(HtmlHelper.ValidationMessageCssClassName));
        await Assertions.Expect(scenario.Page.Locator("#login-email-validation")).Not.ToBeEmptyAsync();
    }

    [Then("nothing is sent to the server")]
    public async Task ThenNothingIsSentToTheServer()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.Login());
        Assert.False(_postSent);
    }

    [Then("the page raises no script error")]
    public void ThenThePageRaisesNoScriptError()
    {
        Assert.Empty(_scriptErrors);
    }

    [Then("they are told the account is locked")]
    public void ThenTheyAreToldTheAccountIsLocked()
    {
        Assert.Contains(PageRoutes.Lockout, scenario.Page.Url, StringComparison.Ordinal);
    }
}
