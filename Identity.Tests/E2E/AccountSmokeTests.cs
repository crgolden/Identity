namespace Identity.Tests.E2E;

using Infrastructure;

[Collection(E2ECollection.Name)]
[Trait("Category", "Smoke")]
public sealed class AccountSmokeTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Account_register_and_delete_lifecycle()
    {
        var email = Environment.GetEnvironmentVariable("TEST_USERNAME") ?? throw new InvalidOperationException("TEST_USERNAME is not set.");
        var password = Environment.GetEnvironmentVariable("TEST_PASSWORD") ?? throw new InvalidOperationException("TEST_PASSWORD is not set.");

        var (ctx, page) = await fixture.NewPageAsync("Smoke");
        await using (ctx)
        {
            // REGISTER — reCAPTCHA bypassed for smoke account; email confirmation sent but not yet confirmed
            await page.GotoAsync("/Account/Register");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("#registerSubmit");
            await page.WaitForURLAsync("**/Account/RegisterConfirmation**");

            // Confirm email directly in the database (no inbox required)
            await fixture.ConfirmUserEmailAsync(email);

            // LOGIN — reCAPTCHA bypassed for smoke account
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", page.Url);

            // DELETE
            await page.GotoAsync("/Account/Manage/DeletePersonalData");
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("button.btn-danger");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Manage"));
        }

        // Verify deletion — login must now fail
        var (ctx2, page2) = await fixture.NewPageAsync("Smoke");
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");
            await page2.WaitForURLAsync("**/Account/Login**");
            var error = await page2.TextContentAsync(".validation-summary-errors, .text-danger");
            Assert.NotNull(error);
        }
    }
}
