namespace Identity.Tests.E2E;

using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Infrastructure;
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
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await SetGoogleClaimsAsync(page, new FakeGoogleClaims
            {
                Sub = Guid.NewGuid().ToString(),
                Email = email,
                EmailVerified = true,
                Name = "Chris Golden",
                GivenName = "Chris",
                Surname = "Golden",
                Picture = "https://example.test/avatar.jpg"
            });

            await page.GotoAsync("/Account/Login");
            await page.ClickAsync("#external-login-button-GoogleOpenIdConnect");

            // email_verified=true skips the confirmation-email round trip entirely.
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/RegisterConfirmation", page.Url, StringComparison.Ordinal);
        }

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);

        var claims = await userManager.GetClaimsAsync(user);

        // Google's sub is not persisted as a UserClaim — it already lives in AspNetUserLogins.ProviderKey,
        // and storing it again under ClaimTypes.NameIdentifier would collide with the claim type
        // UserClaimsPrincipalFactory uses for the user's own ID.
        Assert.DoesNotContain(claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Email && c.Value == email);
        Assert.Contains(claims, c => c.Type == "email_verified" && c.Value == "true");
        Assert.Contains(claims, c => c.Type == "name" && c.Value == "Chris Golden");
        Assert.Contains(claims, c => c.Type == "picture" && c.Value == "https://example.test/avatar.jpg");
        Assert.Contains(claims, c => c.Type == ClaimTypes.GivenName && c.Value == "Chris");
        Assert.Contains(claims, c => c.Type == ClaimTypes.Surname && c.Value == "Golden");

        var logins = await userManager.GetLoginsAsync(user);
        Assert.Contains(logins, l => l.LoginProvider == "GoogleOpenIdConnect");
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

            await page.GotoAsync("/Account/Login");
            await page.ClickAsync("#external-login-button-GoogleOpenIdConnect");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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

            await page.GotoAsync("/Account/Login");
            await page.ClickAsync("#external-login-button-GoogleOpenIdConnect");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var body = await page.TextContentAsync("body");
            Assert.Contains(email, body, StringComparison.Ordinal);
            Assert.Contains("Google", body, StringComparison.Ordinal);
            Assert.Contains("External Logins", body, StringComparison.Ordinal);
        }

        // No second account was created for the same email, and Google was not linked to the existing one.
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        var logins = await userManager.GetLoginsAsync(user);
        Assert.DoesNotContain(logins, l => l.LoginProvider == "GoogleOpenIdConnect");
    }

    [Fact]
    public async Task LinkGoogleToExistingLoggedInUser_AddsMissingClaimsWithoutOverwritingExistingOnes()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        await using (var seedScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var seedUserManager = seedScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var seedUser = await seedUserManager.FindByEmailAsync(email);
            Assert.NotNull(seedUser);

            // Pre-existing claim: must survive the link untouched, even though Google will present
            // a different value for the same claim type.
            await seedUserManager.AddClaimAsync(seedUser, new Claim(ClaimTypes.GivenName, "PreExistingGivenName"));
        }

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await SetGoogleClaimsAsync(page, new FakeGoogleClaims
            {
                Sub = Guid.NewGuid().ToString(),
                Email = email,
                EmailVerified = true,
                GivenName = "GoogleGivenName", // same claim type as the pre-existing one — must NOT overwrite
                Surname = "GoogleSurname" // new claim type — should be added
            });

            await page.GotoAsync("/Account/Manage/ExternalLogins");
            await page.ClickAsync("#link-login-button-GoogleOpenIdConnect");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Manage/ExternalLogins"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var body = await page.TextContentAsync("body");
            Assert.Contains("added", body, StringComparison.OrdinalIgnoreCase);
        }

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        var claims = await userManager.GetClaimsAsync(user);
        Assert.Contains(claims, c => c.Type == ClaimTypes.GivenName && c.Value == "PreExistingGivenName");
        Assert.DoesNotContain(claims, c => c.Type == ClaimTypes.GivenName && c.Value == "GoogleGivenName");
        Assert.Contains(claims, c => c.Type == ClaimTypes.Surname && c.Value == "GoogleSurname");

        var logins = await userManager.GetLoginsAsync(user);
        Assert.Contains(logins, l => l.LoginProvider == "GoogleOpenIdConnect");
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
