namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class PasswordResetTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ForgotPassword_Reset_LoginWithNewPassword_Succeeds()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();
        var newPassword = Generated.NewPassword();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.ForgotPassword);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.ClickAsync("#forgot-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ForgotPasswordConfirmation"));

            var resetEmail = fixture.Email.TakeEmail(email);
            var resetLink = fixture.ExtractEmailLink(resetEmail);

            await page.GotoAsync(resetLink);
            await page.WaitForURLAsync("**/Account/ResetPassword**");
            await page.FillAsync("input[name='Input.Password']", newPassword);
            await page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
            await page.ClickAsync("#reset-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ResetPasswordConfirmation"));

            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", newPassword);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.DoesNotContain(PageRoutes.Login, page.Url, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ForgotPassword_Reset_OldPasswordNoLongerWorks()
    {
        var (email, oldPassword) = await fixture.CreateConfirmedUserAsync();
        var newPassword = Generated.NewPassword();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.ForgotPassword);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.ClickAsync("#forgot-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ForgotPasswordConfirmation"));

            var resetEmail = fixture.Email.TakeEmail(email);
            var resetLink = fixture.ExtractEmailLink(resetEmail);

            await page.GotoAsync(resetLink);
            await page.WaitForURLAsync("**/Account/ResetPassword**");
            await page.FillAsync("input[name='Input.Password']", newPassword);
            await page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
            await page.ClickAsync("#reset-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ResetPasswordConfirmation"));

            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", oldPassword);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(PageRoutes.Login));
            var errorText = await page.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }
}
