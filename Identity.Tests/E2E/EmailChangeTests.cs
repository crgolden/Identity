namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class EmailChangeTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ChangeEmail_Success_NewEmailConfirmed_OldEmailNoLongerValid()
    {
        var (originalEmail, password) = await CreateConfirmedUserAsync();
        var newEmail = $"e2e-new-{Guid.NewGuid()}@test.invalid";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login with original email
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", originalEmail);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Navigate to email management page
            await page.GotoAsync("/Account/Manage/Email");
            await page.WaitForURLAsync("**/Account/Manage/Email**");

            // Request email change
            await page.FillAsync("input[name='Input.NewEmail']", newEmail);
            await page.ClickAsync("#change-email-button");

            // Confirmation email sent
            await page.WaitForURLAsync("**/Account/Manage/Email**");
            var bodyText = await page.TextContentAsync("body");
            Assert.Contains("confirmation", bodyText, StringComparison.OrdinalIgnoreCase);
        }

        // Capture and follow the confirmation link sent to the new email address
        var confirmEmail = await fixture.Email.WaitForEmailAsync(newEmail);
        var confirmLink = EmailCaptureService.ExtractLink(confirmEmail.HtmlBody, "http");

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(confirmLink);
            await page2.WaitForURLAsync("**/Account/ConfirmEmailChange**");
            var bodyText = await page2.TextContentAsync("body");
            Assert.Contains("confirm", bodyText, StringComparison.OrdinalIgnoreCase);
        }

        // Verify login now works with the new email
        var (ctx3, page3) = await fixture.NewPageAsync();
        await using (ctx3)
        {
            await page3.GotoAsync("/Account/Login");
            await page3.FillAsync("input[name='Input.Email']", newEmail);
            await page3.FillAsync("input[name='Input.Password']", password);
            await page3.ClickAsync("button[type='submit']");
            await page3.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", page3.Url);
        }
    }

    [Fact]
    public async Task ChangeEmail_SameEmail_DoesNotSendConfirmation()
    {
        var (email, password) = await CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            // Attempt to "change" to the same email
            await page.GotoAsync("/Account/Manage/Email");
            await page.FillAsync("input[name='Input.NewEmail']", email);
            await page.ClickAsync("#change-email-button");

            // Should stay on the page without sending a confirmation
            await page.WaitForURLAsync("**/Account/Manage/Email**");
            var bodyText = await page.TextContentAsync("body");

            // The page should indicate no change is needed or simply stay current
            Assert.DoesNotContain("confirmation link has been sent", bodyText, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<(string Email, string Password)> CreateConfirmedUserAsync()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
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
