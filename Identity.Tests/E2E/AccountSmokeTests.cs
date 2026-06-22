namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Collection(E2ECollection.Name)]
[Trait("Category", "Smoke")]
public sealed class AccountSmokeTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Account_register_and_delete_lifecycle()
    {
        var email = Environment.GetEnvironmentVariable("TestEmail") ?? throw new InvalidOperationException("TestEmail is not set.");
        var password = Environment.GetEnvironmentVariable("TestPassword") ?? throw new InvalidOperationException("TestPassword is not set.");

        // Clean up any leftover account from a previous partial run before registering.
        await fixture.DeleteUserIfExistsAsync(email);

        var (ctx, page) = await fixture.NewPageAsync("Smoke");
        await using (ctx)
        {
            // REGISTER — reCAPTCHA bypassed for smoke account; email confirmation sent but not yet confirmed
            await page.GotoAsync("/Account/Register");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("#registerSubmit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Confirm email directly in the database (no inbox required)
            await fixture.ConfirmUserEmailAsync(email);

            // LOGIN — reCAPTCHA bypassed for smoke account
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/Login", page.Url);

            // DELETE
            await page.GotoAsync("/Account/Manage/DeletePersonalData");
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#delete-account-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Manage"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        }

        // Verify deletion — login must now fail
        var (ctx2, page2) = await fixture.NewPageAsync("Smoke");
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");
            await Assertions.Expect(page2).ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var error = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(error);
        }
    }
}
