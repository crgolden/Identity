namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AccountManagementTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ChangePassword_OldPasswordNoLongerWorks()
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
    public async Task DeleteAccount_LoginFails()
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
        }

        return (email, password);
    }
}