namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AccountManagementTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ChangePassword_Success_OldPasswordNoLongerWorks()
    {
        var (email, oldPassword) = await CreateAndLoginAsync();
        const string newPassword = "Changed@789012!";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", oldPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Change password
            await page.GotoAsync("/Account/Manage/ChangePassword");
            await page.FillAsync("input[name='Input.OldPassword']", oldPassword);
            await page.FillAsync("input[name='Input.NewPassword']", newPassword);
            await page.FillAsync("input[name='Input.ConfirmPassword']", newPassword);
            await page.ClickAsync("button.btn-primary");

            // Confirm success message
            await page.WaitForURLAsync("**/Account/Manage/ChangePassword**");
            var body = await page.TextContentAsync("body");
            Assert.Contains("changed", body, StringComparison.OrdinalIgnoreCase);
        }

        // Old password should now fail
        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", oldPassword);
            await page2.ClickAsync("button[type='submit']");
            await page2.WaitForURLAsync("**/Account/Login**");
            var errorText = await page2.TextContentAsync(".validation-summary-errors, .text-danger");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task DeleteAccount_Success_SubsequentLoginFails()
    {
        var (email, password) = await CreateAndLoginAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Delete account
            await page.GotoAsync("/Account/Manage/DeletePersonalData");
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button.btn-danger");

            // Should redirect to home/login after deletion
            await page.WaitForURLAsync(url => !url.Contains("/Account/Manage"));
        }

        // Login with deleted account should fail
        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("button[type='submit']");
            await page2.WaitForURLAsync("**/Account/Login**");
            var errorText = await page2.TextContentAsync(".validation-summary-errors, .text-danger");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Logout_Succeeds_ProtectedPageRedirectsToLogin()
    {
        var (email, password) = await CreateAndLoginAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Navigate to logout page — OnGetAsync signs out and redirects immediately
            await page.GotoAsync("/Account/Logout");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Logout"));

            // A protected page should now redirect to login
            await page.GotoAsync("/Account/Manage/Index");
            await page.WaitForURLAsync(url => url.Contains("/Account/Login"));
            Assert.Contains("/Account/Login", page.Url);
        }
    }

    [Fact]
    public async Task ChangeEmail_Succeeds_NewEmailWorks()
    {
        var (oldEmail, password) = await CreateAndLoginAsync();
        var newEmail = $"e2e-changed-{Guid.NewGuid()}@test.invalid";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", oldEmail);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Request email change
            await page.GotoAsync("/Account/Manage/Email");
            await page.FillAsync("input[name='Input.NewEmail']", newEmail);
            await page.ClickAsync("#change-email-button");
            await page.WaitForURLAsync("**/Account/Manage/Email**");

            // Confirm via link sent to the new address
            var changeEmail = await fixture.Email.WaitForEmailAsync(newEmail);
            var changeLink = EmailCaptureService.ExtractLink(changeEmail.HtmlBody, "http");
            await page.GotoAsync(changeLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmailChange**");
        }

        // Login with the new email should now succeed
        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", newEmail);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("button[type='submit']");
            await page2.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", page2.Url);
        }
    }

    [Fact]
    public async Task ResendEmailConfirmation_NewLink_ConfirmsAccount()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        // Register but discard the initial confirmation email
        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync("/Account/Register");
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("button[type='submit']");
            await page1.WaitForURLAsync("**/Account/RegisterConfirmation**");
            await fixture.Email.WaitForEmailAsync(email); // consume without using
        }

        // Resend confirmation and confirm with the new link
        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/ResendEmailConfirmation");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.ClickAsync("button[type='submit']");

            var newConfirmEmail = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureService.ExtractLink(newConfirmEmail.HtmlBody, "http");
            await page2.GotoAsync(confirmLink);
            await page2.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        // Login should now succeed
        var (ctx3, page3) = await fixture.NewPageAsync();
        await using (ctx3)
        {
            await page3.GotoAsync("/Account/Login");
            await page3.FillAsync("input[name='Input.Email']", email);
            await page3.FillAsync("input[name='Input.Password']", password);
            await page3.ClickAsync("button[type='submit']");
            await page3.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", page3.Url);
        }
    }

    private async Task<(string Email, string Password)> CreateAndLoginAsync(string? emailOverride = null)
    {
        var email = emailOverride ?? $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        var (ctx, page) = await fixture.NewPageAsync();
        await using (ctx)
        {
            await page.GotoAsync("/Account/Register");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/Account/RegisterConfirmation**");

            var confirmEmail = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureService.ExtractLink(confirmEmail.HtmlBody, "http");
            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        return (email, password);
    }
}