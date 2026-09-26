namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
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
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", originalEmail);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await page.GotoAsync("/Account/Manage/Email");
            await page.WaitForURLAsync("**/Account/Manage/Email**");

            await page.FillAsync("input[name='Input.NewEmail']", newEmail);
            await page.ClickAsync("#change-email-button");

            await Assertions.Expect(page.Locator("#email-change-link-sent")).ToBeVisibleAsync();
        }

        var confirmEmail = fixture.Email.TakeEmail(newEmail);
        var confirmLink = fixture.ExtractEmailLink(confirmEmail);

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(confirmLink);
            await page2.WaitForURLAsync("**/Account/ConfirmEmailChange**");
        }

        var (ctx3, page3) = await fixture.NewPageAsync();
        await using (ctx3)
        {
            await page3.GotoAsync(PageRoutes.Login);
            await page3.FillAsync("input[name='Input.Email']", newEmail);
            await page3.FillAsync("input[name='Input.Password']", password);
            await page3.ClickAsync("#login-submit");
            await Assertions.Expect(page3).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.DoesNotContain(PageRoutes.Login, page3.Url, StringComparison.Ordinal);
        }

        var (ctx4, page4) = await fixture.NewPageAsync();
        await using (ctx4)
        {
            await page4.GotoAsync(PageRoutes.Login);
            await page4.FillAsync("input[name='Input.Email']", originalEmail);
            await page4.FillAsync("input[name='Input.Password']", password);
            await page4.ClickAsync("#login-submit");
            await Assertions.Expect(page4).ToHaveURLAsync(new Regex(PageRoutes.Login));
            var errorText = await page4.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task ChangeEmail_SameEmail_DoesNotSendConfirmation()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await page.GotoAsync("/Account/Manage/Email");
            await page.FillAsync("input[name='Input.NewEmail']", email);
            await page.ClickAsync("#change-email-button");

            await Assertions.Expect(page.Locator("#email-unchanged")).ToBeVisibleAsync();
            Assert.False(
                fixture.Email.HasEmailFor(email),
                "Changing to the address already on the account must not enqueue a confirmation email.");
        }
    }
}
