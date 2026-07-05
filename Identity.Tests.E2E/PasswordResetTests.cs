namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class PasswordResetTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ForgotPassword_Reset_LoginWithNewPassword_Succeeds()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();
        const string newPassword = "NewTest@789012!";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Request password reset
            await page.GotoAsync("/Account/ForgotPassword");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.ClickAsync("#forgot-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ForgotPasswordConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Extract reset link
            var resetEmail = await fixture.Email.WaitForEmailAsync(email);
            var resetLink = EmailCaptureSender.ExtractLink(resetEmail.HtmlBody, "http");

            // Navigate to reset link and set new password
            await page.GotoAsync(resetLink);
            await page.WaitForURLAsync("**/Account/ResetPassword**");
            await page.FillAsync("input[name='Input.Password']", newPassword);
            await page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
            await page.ClickAsync("#reset-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ResetPasswordConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Login with new password should succeed
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", newPassword);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/Login", page.Url);
        }
    }

    [Fact]
    public async Task ForgotPassword_Reset_OldPasswordNoLongerWorks()
    {
        var (email, oldPassword) = await fixture.CreateConfirmedUserAsync();
        const string newPassword = "NewTest@789012!";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/ForgotPassword");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.ClickAsync("#forgot-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ForgotPasswordConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            var resetEmail = await fixture.Email.WaitForEmailAsync(email);
            var resetLink = EmailCaptureSender.ExtractLink(resetEmail.HtmlBody, "http");

            await page.GotoAsync(resetLink);
            await page.WaitForURLAsync("**/Account/ResetPassword**");
            await page.FillAsync("input[name='Input.Password']", newPassword);
            await page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
            await page.ClickAsync("#reset-password-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/ResetPasswordConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Old password should now fail
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", oldPassword);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var errorText = await page.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }
}
