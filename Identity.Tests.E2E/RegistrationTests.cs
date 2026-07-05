namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class RegistrationTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Register_DuplicateEmail_ShowsError()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        // First registration succeeds
        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync("/Account/Register");
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        }

        // Second registration with the same email should show an error
        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Register");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page2.ClickAsync("#registerSubmit");

            await Assertions.Expect(page2).ToHaveURLAsync(new Regex("/Account/Register"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Register_UnconfirmedEmail_LoginShowsError()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        // Register but do not follow the confirmation link
        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync("/Account/Register");
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        }

        // Login attempt with unconfirmed email should fail
        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");

            await Assertions.Expect(page2).ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

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
            await page.ClickAsync("#registerSubmit");

            // Should land on the "confirm your email" page
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Get confirmation link from captured email
            var captured = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureSender.ExtractLink(captured.HtmlBody, "http");

            // Navigate to confirmation link
            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");

            // Now login
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");

            // Should redirect away from login (authenticated)
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/Login", page.Url);
        }
    }
}
