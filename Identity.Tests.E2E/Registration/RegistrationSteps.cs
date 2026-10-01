namespace Identity.Tests.E2E.Registration;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class RegistrationSteps(BrowserScenario scenario, Member member)
{
    private IPage? _secondVisitorPage;

    private IPage SecondVisitorPage => _secondVisitorPage ?? throw new InvalidOperationException("No second visitor registered in this scenario.");

    [When("a visitor registers an account")]
    [Given("a visitor has registered an account")]
    public async Task AVisitorRegistersAnAccount()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        member.Remember(email, Generated.NewPassword());
        await RegisterAsync(scenario.Page);
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.RegisterConfirmation());
    }

    [Given("they have set aside the first confirmation message")]
    public void GivenTheyHaveSetAsideTheFirstConfirmationMessage()
    {
        scenario.Fixture.Email.TakeEmail(member.Email);
    }

    [When("they confirm their email from the message they were sent")]
    public async Task WhenTheyConfirmTheirEmailFromTheMessageTheyWereSent()
    {
        var confirmLink = scenario.Fixture.ExtractEmailLink(scenario.Fixture.Email.TakeEmail(member.Email));
        await scenario.Page.GotoAsync(confirmLink);
        await scenario.Page.WaitForURLAsync("**/Account/ConfirmEmail**");
    }

    [When("another visitor registers with the same email")]
    public async Task WhenAnotherVisitorRegistersWithTheSameEmail()
    {
        _secondVisitorPage = await scenario.OpenAnotherPageAsync();
        await RegisterAsync(SecondVisitorPage);
    }

    [When("they ask for another confirmation email")]
    public async Task WhenTheyAskForAnotherConfirmationEmail()
    {
        await scenario.Page.GotoAsync(PageRoutes.ResendEmailConfirmation);
        await scenario.Page.FillAsync("input[name='Input.Email']", member.Email);
        await scenario.Page.RunAndWaitForResponseAsync(
            () => scenario.Page.ClickAsync("#resend-email-submit"),
            response => string.Equals(response.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal)
                        && response.Url.Contains(PageRoutes.ResendEmailConfirmation, StringComparison.OrdinalIgnoreCase));
    }

    [Then("the registration is refused with the reason shown")]
    public async Task ThenTheRegistrationIsRefusedWithTheReasonShown()
    {
        await Assertions.Expect(SecondVisitorPage).ToHaveURLAsync(UrlPatterns.Register());
        Assert.NotNull(await SecondVisitorPage.TextContentAsync("#validation-errors"));
    }

    private async Task RegisterAsync(IPage page)
    {
        await page.GotoAsync(PageRoutes.Register);
        await page.FillAsync("input[name='Input.Email']", member.Email);
        await page.FillAsync("input[name='Input.Password']", member.Password);
        await page.FillAsync("input[name='Input.ConfirmPassword']", member.Password);
        await page.ClickAsync("#registerSubmit");
    }
}
