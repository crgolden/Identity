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

        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync("/Account/Register");
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Register");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page2.ClickAsync("#registerSubmit");

            await Assertions.Expect(page2).ToHaveURLAsync(new Regex("/Account/Register"));
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Register_UnconfirmedEmail_LoginShowsError()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync("/Account/Register");
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync("/Account/Login");
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");

            await Assertions.Expect(page2).ToHaveURLAsync(new Regex("/Account/Login"));
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
            await page.GotoAsync("/Account/Register");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("#registerSubmit");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));

            var captured = fixture.Email.TakeEmail(email);
            var confirmLink = EmailCaptureSender.ExtractLink(captured.HtmlBody, "http");

            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");

            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");

            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"));
            Assert.DoesNotContain("/Account/Login", page.Url, StringComparison.Ordinal);
        }
    }
}