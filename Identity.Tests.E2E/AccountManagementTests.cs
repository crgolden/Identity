namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AccountManagementTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ChangePassword_Success_OldPasswordNoLongerWorks()
    {
        var (email, oldPassword) = await fixture.CreateConfirmedUserAsync();
        const string newPassword = "Changed@789012!";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", oldPassword);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Manage/ChangePassword");
            await page.FillAsync("input[name='Input.OldPassword']", oldPassword);
            await page.FillAsync("input[name='Input.NewPassword']", newPassword);
            await page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
            await page.ClickAsync("#change-password-submit");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Manage/ChangePassword"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var body = await page.TextContentAsync("body");
            Assert.Contains("changed", body, StringComparison.OrdinalIgnoreCase);
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", oldPassword);
            await page2.ClickAsync("#login-submit");
            await Assertions.Expect(page2).ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task DeleteAccount_Success_SubsequentLoginFails()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Manage/DeletePersonalData");
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#delete-account-submit");

            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Manage"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");
            await Assertions.Expect(page2).ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Logout_Succeeds_ProtectedPageRedirectsToLogin()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Logout");
            await page.ClickAsync("#logout-submit");
            await page.WaitForLoadStateAsync();

            await page.GotoAsync("/Account/Manage/Index");
            await page.WaitForURLAsync(url => url.Contains("/Account/Login"));
            Assert.Contains("/Account/Login", page.Url);
        }
    }

    [Fact]
    public async Task ChangeEmail_Succeeds_NewEmailWorks()
    {
        var (oldEmail, password) = await fixture.CreateConfirmedUserAsync();
        var newEmail = $"e2e-changed-{Guid.NewGuid()}@test.invalid";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", oldEmail);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Manage/Email");
            await page.FillAsync("input[name='Input.NewEmail']", newEmail);
            await page.ClickAsync("#change-email-button");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Manage/Email"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            var changeEmail = await fixture.Email.WaitForEmailAsync(newEmail);
            var changeLink = EmailCaptureSender.ExtractLink(changeEmail.HtmlBody, "http");
            await page.GotoAsync(changeLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmailChange**");
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", newEmail);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");
            await Assertions.Expect(page2).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/Login", page2.Url);
        }
    }

    [Fact]
    public async Task ResendEmailConfirmation_NewLink_ConfirmsAccount()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync("/Account/Register");
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await fixture.Email.WaitForEmailAsync(email);
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/ResendEmailConfirmation");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.ClickAsync("#resend-email-submit");

            var newConfirmEmail = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureSender.ExtractLink(newConfirmEmail.HtmlBody, "http");
            await page2.GotoAsync(confirmLink);
            await page2.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        var (ctx3, page3) = await fixture.NewPageAsync();
        await using (ctx3)
        {
            await page3.GotoAsync("/Account/Login");
            await page3.FillAsync("input[name='Input.Email']", email);
            await page3.FillAsync("input[name='Input.Password']", password);
            await page3.ClickAsync("#login-submit");
            await Assertions.Expect(page3).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/Login", page3.Url);
        }
    }
}