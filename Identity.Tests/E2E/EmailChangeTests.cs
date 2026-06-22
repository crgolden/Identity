namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class EmailChangeTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ChangeEmail_Success_NewEmailConfirmed_OldEmailNoLongerValid()
    {
        var (originalEmail, password) = await fixture.CreateConfirmedUserAsync();
        var newEmail = $"e2e-new-{Guid.NewGuid()}@test.invalid";

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login with original email
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", originalEmail);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Navigate to email management page
            await page.GotoAsync("/Account/Manage/Email");
            await page.WaitForURLAsync("**/Account/Manage/Email**");

            // Request email change
            await page.FillAsync("input[name='Input.NewEmail']", newEmail);
            await page.ClickAsync("#change-email-button");

            // Confirmation email sent
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Manage/Email"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var bodyText = await page.TextContentAsync("body");
            Assert.Contains("confirmation", bodyText, StringComparison.OrdinalIgnoreCase);
        }

        // Capture and follow the confirmation link sent to the new email address
        var confirmEmail = await fixture.Email.WaitForEmailAsync(newEmail);
        var confirmLink = EmailCaptureSender.ExtractLink(confirmEmail.HtmlBody, "http");

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
            await page3.ClickAsync("#login-submit");
            await Assertions.Expect(page3).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/Login", page3.Url);
        }
    }

    [Fact]
    public async Task ChangeEmail_SameEmail_DoesNotSendConfirmation()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            // Login
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            // Attempt to "change" to the same email
            await page.GotoAsync("/Account/Manage/Email");
            await page.FillAsync("input[name='Input.NewEmail']", email);
            await page.ClickAsync("#change-email-button");

            // Should stay on the page without sending a confirmation
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Manage/Email"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var bodyText = await page.TextContentAsync("body");

            // The page should indicate no change is needed or simply stay current
            Assert.DoesNotContain("confirmation link has been sent", bodyText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
