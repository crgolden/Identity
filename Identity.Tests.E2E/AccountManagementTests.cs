namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AccountManagementTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ChangePassword_Success_OldPasswordNoLongerWorks()
    {
        var (email, oldPassword) = await fixture.CreateConfirmedUserAsync();
        var newPassword = Generated.NewPassword();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", oldPassword);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await page.GotoAsync("/Account/Manage/ChangePassword");
            await page.FillAsync("input[name='Input.OldPassword']", oldPassword);
            await page.FillAsync("input[name='Input.NewPassword']", newPassword);
            await page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
            await page.ClickAsync("#change-password-submit");
            await Assertions.Expect(page.Locator("#password-changed")).ToBeVisibleAsync();
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(PageRoutes.Login);
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", oldPassword);
            await page2.ClickAsync("#login-submit");
            await Assertions.Expect(page2).ToHaveURLAsync(new Regex(PageRoutes.Login));
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }

        var (ctx3, page3) = await fixture.NewPageAsync();
        await using (ctx3)
        {
            await page3.GotoAsync(PageRoutes.Login);
            await page3.FillAsync("input[name='Input.Email']", email);
            await page3.FillAsync("input[name='Input.Password']", newPassword);
            await page3.ClickAsync("#login-submit");
            await Assertions.Expect(page3).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.DoesNotContain(PageRoutes.Login, page3.Url, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task DeleteAccount_Success_SubsequentLoginFails()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await page.GotoAsync("/Account/Manage/DeletePersonalData");
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#delete-account-submit");

            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Manage"));
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(PageRoutes.Login);
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");
            await Assertions.Expect(page2).ToHaveURLAsync(new Regex(PageRoutes.Login));
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
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await page.GotoAsync(PageRoutes.Logout);
            await page.ClickAsync("#logout-submit");
            await page.WaitForLoadStateAsync();

            await page.GotoAsync("/Account/Manage/Index");
            await page.WaitForURLAsync(url => url.Contains(PageRoutes.Login, StringComparison.Ordinal));
            Assert.Contains(PageRoutes.Login, page.Url, StringComparison.Ordinal);
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
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", oldEmail);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await page.GotoAsync("/Account/Manage/Email");
            await page.FillAsync("input[name='Input.NewEmail']", newEmail);
            await page.ClickAsync("#change-email-button");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Manage/Email"));

            var changeEmail = fixture.Email.TakeEmail(newEmail);
            var changeLink = fixture.ExtractEmailLink(changeEmail);
            await page.GotoAsync(changeLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmailChange**");
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(PageRoutes.Login);
            await page2.FillAsync("input[name='Input.Email']", newEmail);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");
            await Assertions.Expect(page2).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.DoesNotContain(PageRoutes.Login, page2.Url, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ResendEmailConfirmation_NewLink_ConfirmsAccount()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        var password = Generated.NewPassword();

        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync(PageRoutes.Register);
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));
            fixture.Email.TakeEmail(email);
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(PageRoutes.ResendEmailConfirmation);
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.RunAndWaitForResponseAsync(
                () => page2.ClickAsync("#resend-email-submit"),
                response => string.Equals(response.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal)
                            && response.Url.Contains(PageRoutes.ResendEmailConfirmation, StringComparison.OrdinalIgnoreCase));

            var newConfirmEmail = fixture.Email.TakeEmail(email);
            var confirmLink = fixture.ExtractEmailLink(newConfirmEmail);
            await page2.GotoAsync(confirmLink);
            await page2.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        var (ctx3, page3) = await fixture.NewPageAsync();
        await using (ctx3)
        {
            await page3.GotoAsync(PageRoutes.Login);
            await page3.FillAsync("input[name='Input.Email']", email);
            await page3.FillAsync("input[name='Input.Password']", password);
            await page3.ClickAsync("#login-submit");
            await Assertions.Expect(page3).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.DoesNotContain(PageRoutes.Login, page3.Url, StringComparison.Ordinal);
        }
    }
}
