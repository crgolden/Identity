namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
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
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");

            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            Assert.DoesNotContain("/Account/Login", page.Url);
        }
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", "WrongPassword!99");
            await page.ClickAsync("#login-submit");

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            var errorText = await page.TextContentAsync("#validation-errors");
            Assert.NotNull(errorText);
        }
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccount()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");

            for (var i = 0; i < 5; i++)
            {
                await page.FillAsync("input[name='Input.Email']", email);
                await page.FillAsync("input[name='Input.Password']", "BadPassword!99");

                var postResponse = page.WaitForResponseAsync(
                    res => res.Request.Method == "POST" && res.Url.Contains("/Account/Login"));
                await page.ClickAsync("#login-submit");
                await postResponse;

                if (i < 4)
                {
                    await page.Locator("input[name='Input.Email']").WaitForAsync();
                }
                else
                {
                    await page.Locator("#lockout-heading").WaitForAsync();
                }
            }

            Assert.Contains("/Account/Lockout", page.Url);
        }
    }
}