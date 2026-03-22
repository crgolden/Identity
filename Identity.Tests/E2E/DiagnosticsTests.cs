namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class DiagnosticsTests(PlaywrightFixture fixture)
{
    /// <summary>Verifies that an authenticated user can access the diagnostics page and sees claims.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Diagnostics_AuthenticatedUser_ShowsClaims()
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

            await page.GotoAsync("/Diagnostics/Index");
            await page.WaitForLoadStateAsync();

            var content = await page.ContentAsync();
            Assert.Contains("sub", content, StringComparison.OrdinalIgnoreCase);
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
