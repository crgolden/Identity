namespace Identity.Tests.E2E;

using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Apis.Auth.AspNetCore3;
using Identity.Avatar;
using Identity.Pages.Account;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ExternalLoginTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Register_NewGoogleAccount_EmailVerified_CreatesAccountSignsInAndPersistsAllClaims()
    {
        var email = $"e2e-google-{Guid.NewGuid()}@test.invalid";
        var givenName = Generated.NewPersonName();
        var surname = Generated.NewPersonName();
        var fullName = $"{givenName} {surname}";
        var pictureUrl = Generated.NewPictureAddress();
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await SetGoogleClaimsAsync(page, new FakeGoogleClaims
            {
                Sub = Guid.NewGuid().ToString(),
                Email = email,
                EmailVerified = true,
                Name = fullName,
                GivenName = givenName,
                Surname = surname,
                Picture = pictureUrl
            });

            await page.GotoAsync(PageRoutes.Login);
            await page.ClickAsync("#external-login-button-GoogleOpenIdConnect");

            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.DoesNotContain("/Account/RegisterConfirmation", page.Url, StringComparison.Ordinal);
        }

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);

        var claims = await userManager.GetClaimsAsync(user);
        Assert.DoesNotContain(claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Email && c.Value == email);
        Assert.Contains(claims, c => string.Equals(c.Type, ExternalLogin.EmailVerifiedClaimType, StringComparison.Ordinal) && string.Equals(c.Value, OidcStandardConstants.ClaimValueTrue, StringComparison.Ordinal));
        Assert.Contains(claims, c => string.Equals(c.Type, OidcStandardConstants.NameClaim, StringComparison.Ordinal) && string.Equals(c.Value, fullName, StringComparison.Ordinal));
        Assert.Contains(claims, c => string.Equals(c.Type, AvatarProfileService.PictureClaimType, StringComparison.Ordinal) && string.Equals(c.Value, pictureUrl, StringComparison.Ordinal));
        Assert.Contains(claims, c => c.Type == ClaimTypes.GivenName && string.Equals(c.Value, givenName, StringComparison.Ordinal));
        Assert.Contains(claims, c => c.Type == ClaimTypes.Surname && string.Equals(c.Value, surname, StringComparison.Ordinal));

        var logins = await userManager.GetLoginsAsync(user);
        Assert.Contains(logins, l => string.Equals(l.LoginProvider, GoogleOpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Register_NewGoogleAccount_EmailNotVerified_RequiresConfirmationEmail()
    {
        var email = $"e2e-google-{Guid.NewGuid()}@test.invalid";
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await SetGoogleClaimsAsync(page, new FakeGoogleClaims
            {
                Sub = Guid.NewGuid().ToString(),
                Email = email,
                EmailVerified = false
            });

            await page.GotoAsync(PageRoutes.Login);
            await page.ClickAsync("#external-login-button-GoogleOpenIdConnect");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));
        }

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task Register_GoogleAccount_EmailAlreadyRegistered_BlocksRegistrationAndInstructsToLinkInstead()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await SetGoogleClaimsAsync(page, new FakeGoogleClaims
            {
                Sub = Guid.NewGuid().ToString(),
                Email = email,
                EmailVerified = true
            });

            await page.GotoAsync(PageRoutes.Login);
            await page.ClickAsync("#external-login-button-GoogleOpenIdConnect");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(PageRoutes.Login));
            await Assertions.Expect(page.Locator("#validation-errors")).ToHaveAttributeAsync("data-error-code", ExternalLogin.AccountAlreadyExistsErrorCode);
        }

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        var logins = await userManager.GetLoginsAsync(user);
        Assert.DoesNotContain(logins, l => string.Equals(l.LoginProvider, GoogleOpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal));
    }

    [Fact]
    public async Task LinkGoogleToExistingLoggedInUser_AddsMissingClaimsWithoutOverwritingExistingOnes()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();
        var preExistingGivenName = Generated.NewPersonName();
        var googleGivenName = Generated.NewPersonName();
        var googleSurname = Generated.NewPersonName();

        await using (var seedScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var seedUserManager = seedScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var seedUser = await seedUserManager.FindByEmailAsync(email);
            Assert.NotNull(seedUser);
            await seedUserManager.AddClaimAsync(seedUser, new Claim(ClaimTypes.GivenName, preExistingGivenName));
        }

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await SetGoogleClaimsAsync(page, new FakeGoogleClaims
            {
                Sub = Guid.NewGuid().ToString(),
                Email = email,
                EmailVerified = true,
                GivenName = googleGivenName,
                Surname = googleSurname
            });

            await page.GotoAsync(Pages.Account.Manage.ExternalLogins.ExternalLoginsPagePath);
            await page.ClickAsync("#link-login-button-GoogleOpenIdConnect");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Account.Manage.ExternalLogins.ExternalLoginsPagePath));
        }

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        var claims = await userManager.GetClaimsAsync(user);
        Assert.Contains(claims, c => c.Type == ClaimTypes.GivenName && string.Equals(c.Value, preExistingGivenName, StringComparison.Ordinal));
        Assert.DoesNotContain(claims, c => c.Type == ClaimTypes.GivenName && string.Equals(c.Value, googleGivenName, StringComparison.Ordinal));
        Assert.Contains(claims, c => c.Type == ClaimTypes.Surname && string.Equals(c.Value, googleSurname, StringComparison.Ordinal));

        var logins = await userManager.GetLoginsAsync(user);
        Assert.Contains(logins, l => string.Equals(l.LoginProvider, GoogleOpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal));
    }

    private async Task SetGoogleClaimsAsync(IPage page, FakeGoogleClaims claims)
    {
        var json = JsonSerializer.Serialize(claims);
        var cookie = new Cookie
        {
            Name = FakeExternalAuthenticationHandler.ClaimsCookieName,
            Value = Uri.EscapeDataString(json),
            Url = fixture.BaseAddress
        };
        await page.Context.AddCookiesAsync([cookie]);
    }
}
