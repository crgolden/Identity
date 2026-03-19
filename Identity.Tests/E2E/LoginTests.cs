namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class LoginTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Login_ValidCredentials_Succeeds()
    {
        var (email, password) = await CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button[type='submit']");

            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", page.Url);
        }
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var (email, _) = await CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", "WrongPassword!99");
            await page.ClickAsync("button[type='submit']");

            // Should stay on login page with a validation error
            await page.WaitForURLAsync("**/Account/Login**");
            var errorText = await page.TextContentAsync(".validation-summary-errors, .text-danger");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Login_Lockout_AfterFiveFailedAttempts()
    {
        var (email, _) = await CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            for (var i = 0; i < 5; i++)
            {
                await page.GotoAsync("/Account/Login");
                await page.FillAsync("input[name='Input.Email']", email);
                await page.FillAsync("input[name='Input.Password']", "BadPassword!99");
                await page.ClickAsync("button[type='submit']");
            }

            // After 5 failures the account should be locked out
            await page.WaitForURLAsync("**/Account/Lockout**");
            Assert.Contains("/Account/Lockout", page.Url);
        }
    }

    /// <summary>Registers and confirms an account, then returns email+password.</summary>
    private async Task<(string Email, string Password)> CreateConfirmedUserAsync()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Register");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/Account/RegisterConfirmation**");

            var captured = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureService.ExtractLink(captured.HtmlBody, "http");
            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        return (email, password);
    }
}