namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class RegistrationTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Register_ConfirmEmail_Login_Succeeds()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Navigate to registration page
            await page.GotoAsync("/Account/Register");

            // Fill and submit the registration form
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("button[type='submit']");

            // Should land on the "confirm your email" page
            await page.WaitForURLAsync("**/Account/RegisterConfirmation**");

            // Get confirmation link from captured email
            var captured = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureService.ExtractLink(captured.HtmlBody, "http");

            // Navigate to confirmation link
            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");

            // Now login
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");

            // Should redirect away from login (authenticated)
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", page.Url);
        }
    }
}