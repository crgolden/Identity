namespace Identity.Tests.E2E.GoogleSignIn;

using System.Security.Claims;
using System.Text.Json;
using Google.Apis.Auth.AspNetCore3;
using Identity.Avatar;
using Identity.Pages.Account;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class GoogleSignInSteps(BrowserScenario scenario, Member member)
{
    private FakeGoogleClaims? _google;
    private string? _preExistingGivenName;

    private FakeGoogleClaims Google => _google ?? throw new InvalidOperationException("No Google account was prepared in this scenario.");

    private string PreExistingGivenName => _preExistingGivenName ?? throw new InvalidOperationException("The member was given no name in this scenario.");

    [Given("a Google account with a verified email and a full profile")]
    public void GivenAGoogleAccountWithAVerifiedEmailAndAFullProfile()
    {
        var givenName = Generated.NewPersonName();
        var surname = Generated.NewPersonName();
        _google = new FakeGoogleClaims
        {
            Sub = Generated.NewExternalSubject(),
            Email = $"e2e-google-{Guid.NewGuid()}@test.invalid",
            EmailVerified = true,
            Name = $"{givenName} {surname}",
            GivenName = givenName,
            Surname = surname,
            Picture = Generated.NewPictureAddress(),
        };
    }

    [Given("a Google account whose email is not verified")]
    public void GivenAGoogleAccountWhoseEmailIsNotVerified()
    {
        _google = new FakeGoogleClaims
        {
            Sub = Generated.NewExternalSubject(),
            Email = $"e2e-google-{Guid.NewGuid()}@test.invalid",
            EmailVerified = false,
        };
    }

    [Given("a Google account with the member's email")]
    public void GivenAGoogleAccountWithTheMembersEmail()
    {
        _google = new FakeGoogleClaims { Sub = Generated.NewExternalSubject(), Email = member.Email, EmailVerified = true };
    }

    [Given("the member already has a given name")]
    public async Task GivenTheMemberAlreadyHasAGivenName()
    {
        _preExistingGivenName = Generated.NewPersonName();
        await using var scope = scenario.Fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(member.Email);
        Assert.NotNull(user);
        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.GivenName, PreExistingGivenName));
    }

    [Given("a Google account with the member's email, another given name and a surname")]
    public void GivenAGoogleAccountWithTheMembersEmailAnotherGivenNameAndASurname()
    {
        _google = new FakeGoogleClaims
        {
            Sub = Generated.NewExternalSubject(),
            Email = member.Email,
            EmailVerified = true,
            GivenName = Generated.NewPersonName(),
            Surname = Generated.NewPersonName(),
        };
    }

    [When("they sign up with that Google account")]
    public async Task WhenTheySignUpWithThatGoogleAccount()
    {
        await UseGoogleAccountAsync();
        await scenario.Page.GotoAsync(PageRoutes.Login);
        await scenario.Page.ClickAsync("#external-login-button-GoogleOpenIdConnect");
    }

    [When("they link that Google account from their account")]
    public async Task WhenTheyLinkThatGoogleAccountFromTheirAccount()
    {
        await UseGoogleAccountAsync();
        await scenario.Page.GotoAsync(Pages.Account.Manage.ExternalLogins.ExternalLoginsPagePath);
        await scenario.Page.ClickAsync("#link-login-button-GoogleOpenIdConnect");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.ManageExternalLogins());
    }

    [Then("their new account keeps the Google profile")]
    public async Task ThenTheirNewAccountKeepsTheGoogleProfile()
    {
        Assert.DoesNotContain("/Account/RegisterConfirmation", scenario.Page.Url, StringComparison.Ordinal);
        var email = Google.Email;
        Assert.NotNull(email);
        var account = await LoadAccountAsync(email);
        Assert.True(account.User.EmailConfirmed);
        Assert.DoesNotContain(account.Claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(account.Claims, c => c.Type == ClaimTypes.Email && c.Value == Google.Email);
        Assert.Contains(account.Claims, c => string.Equals(c.Type, ExternalLogin.EmailVerifiedClaimType, StringComparison.Ordinal) && string.Equals(c.Value, OidcStandardConstants.ClaimValueTrue, StringComparison.Ordinal));
        Assert.Contains(account.Claims, c => string.Equals(c.Type, OidcStandardConstants.NameClaim, StringComparison.Ordinal) && string.Equals(c.Value, Google.Name, StringComparison.Ordinal));
        Assert.Contains(account.Claims, c => string.Equals(c.Type, AvatarProfileService.PictureClaimType, StringComparison.Ordinal) && string.Equals(c.Value, Google.Picture, StringComparison.Ordinal));
        Assert.Contains(account.Claims, c => c.Type == ClaimTypes.GivenName && string.Equals(c.Value, Google.GivenName, StringComparison.Ordinal));
        Assert.Contains(account.Claims, c => c.Type == ClaimTypes.Surname && string.Equals(c.Value, Google.Surname, StringComparison.Ordinal));
        Assert.Contains(account.Logins, l => string.Equals(l.LoginProvider, GoogleOpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal));
    }

    [Then("they are asked to confirm their email")]
    public async Task ThenTheyAreAskedToConfirmTheirEmail()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.RegisterConfirmation());
    }

    [Then("their new account is not yet confirmed")]
    public async Task ThenTheirNewAccountIsNotYetConfirmed()
    {
        var email = Google.Email;
        Assert.NotNull(email);
        var account = await LoadAccountAsync(email);
        Assert.False(account.User.EmailConfirmed);
    }

    [Then("they are told to link Google to their existing account instead")]
    public async Task ThenTheyAreToldToLinkGoogleToTheirExistingAccountInstead()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.Login());
        await Assertions.Expect(scenario.Page.Locator("#validation-errors")).ToHaveAttributeAsync("data-error-code", ExternalLogin.AccountAlreadyExistsErrorCode);
    }

    [Then("their account has no Google login")]
    public async Task ThenTheirAccountHasNoGoogleLogin()
    {
        var account = await LoadAccountAsync(member.Email);
        Assert.DoesNotContain(account.Logins, l => string.Equals(l.LoginProvider, GoogleOpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal));
    }

    [Then("their account gains a Google login and the surname")]
    public async Task ThenTheirAccountGainsAGoogleLoginAndTheSurname()
    {
        var account = await LoadAccountAsync(member.Email);
        Assert.Contains(account.Claims, c => c.Type == ClaimTypes.Surname && string.Equals(c.Value, Google.Surname, StringComparison.Ordinal));
        Assert.Contains(account.Logins, l => string.Equals(l.LoginProvider, GoogleOpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal));
    }

    [Then("the given name they already had is kept")]
    public async Task ThenTheGivenNameTheyAlreadyHadIsKept()
    {
        var account = await LoadAccountAsync(member.Email);
        Assert.Contains(account.Claims, c => c.Type == ClaimTypes.GivenName && string.Equals(c.Value, PreExistingGivenName, StringComparison.Ordinal));
        Assert.DoesNotContain(account.Claims, c => c.Type == ClaimTypes.GivenName && string.Equals(c.Value, Google.GivenName, StringComparison.Ordinal));
    }

    private async Task UseGoogleAccountAsync()
    {
        var cookie = new Cookie
        {
            Name = FakeExternalAuthenticationHandler.ClaimsCookieName,
            Value = Uri.EscapeDataString(JsonSerializer.Serialize(Google)),
            Url = scenario.Fixture.BaseAddress,
        };
        await scenario.Page.Context.AddCookiesAsync([cookie]);
    }

    private async Task<(IdentityUser<Guid> User, IList<Claim> Claims, IList<UserLoginInfo> Logins)> LoadAccountAsync(string email)
    {
        await using var scope = scenario.Fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        return (user, await userManager.GetClaimsAsync(user), await userManager.GetLoginsAsync(user));
    }
}
