namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class LoginTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Login_ValidCredentials_Succeeds()
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
            Assert.DoesNotContain(PageRoutes.Login, page.Url, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", Generated.NewPassword());
            await page.ClickAsync("#login-submit");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex(PageRoutes.Login));
            var errorText = await page.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Login_EmptySubmit_RendersClientValidationWithoutAScriptError()
    {
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            var scriptErrors = new List<string>();
            page.PageError += (_, error) => scriptErrors.Add(error);
            await page.GotoAsync(PageRoutes.Login);
            await page.WaitForFunctionAsync(BrowserScripts.LoginFormValidatorAttached);

            var postSent = false;
            page.Request += (_, request) => postSent |= string.Equals(request.Method, HttpMethod.Post.Method, StringComparison.Ordinal);
            await page.ClickAsync("#login-submit");

            await Assertions.Expect(page.Locator("#login-email-validation")).ToHaveClassAsync(new Regex(HtmlHelper.ValidationMessageCssClassName));
            await Assertions.Expect(page.Locator("#login-email-validation")).Not.ToBeEmptyAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(PageRoutes.Login));
            Assert.False(postSent);
            Assert.Empty(scriptErrors);
        }
    }

    [Fact]
    public async Task Login_MaxFailedAttempts_LocksAccount()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();
        var maxFailedAttempts = fixture.Factory.Services
            .GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);

            for (var attempt = 1; attempt <= maxFailedAttempts; attempt++)
            {
                await page.FillAsync("input[name='Input.Email']", email);
                await page.FillAsync("input[name='Input.Password']", Generated.NewPassword());

                var postResponse = page.WaitForResponseAsync(
                    res => string.Equals(res.Request.Method, HttpMethod.Post.Method, StringComparison.Ordinal) && res.Url.Contains(PageRoutes.Login, StringComparison.Ordinal));
                await page.ClickAsync("#login-submit");
                await postResponse;

                if (attempt < maxFailedAttempts)
                {
                    await page.Locator("input[name='Input.Email']").WaitForAsync();
                }
                else
                {
                    await page.Locator("#lockout-heading").WaitForAsync();
                }
            }

            Assert.Contains(PageRoutes.Lockout, page.Url, StringComparison.Ordinal);
        }
    }
}
