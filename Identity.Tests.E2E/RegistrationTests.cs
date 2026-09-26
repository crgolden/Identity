namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class RegistrationTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Register_DuplicateEmail_ShowsError()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        var password = Generated.NewPassword();

        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync(PageRoutes.Register);
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(PageRoutes.Register);
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page2.ClickAsync("#registerSubmit");

            await Assertions.Expect(page2).ToHaveURLAsync(new Regex(PageRoutes.Register));
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Register_UnconfirmedEmail_LoginShowsError()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        var password = Generated.NewPassword();

        var (ctx1, page1) = await fixture.NewPageAsync();
        await using (ctx1)
        {
            await page1.GotoAsync(PageRoutes.Register);
            await page1.FillAsync("input[name='Input.Email']", email);
            await page1.FillAsync("input[name='Input.Password']", password);
            await page1.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page1.ClickAsync("#registerSubmit");
            await Assertions.Expect(page1).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));
        }

        var (ctx2, page2) = await fixture.NewPageAsync();
        await using (ctx2)
        {
            await page2.GotoAsync(PageRoutes.Login);
            await page2.FillAsync("input[name='Input.Email']", email);
            await page2.FillAsync("input[name='Input.Password']", password);
            await page2.ClickAsync("#login-submit");

            await Assertions.Expect(page2).ToHaveURLAsync(new Regex(PageRoutes.Login));
            var errorText = await page2.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Register_ConfirmEmail_Login_Succeeds()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        var password = Generated.NewPassword();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Register);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("#registerSubmit");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/RegisterConfirmation"));

            var captured = fixture.Email.TakeEmail(email);
            var confirmLink = fixture.ExtractEmailLink(captured);

            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");

            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");

            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.DoesNotContain(PageRoutes.Login, page.Url, StringComparison.Ordinal);
        }
    }
}
