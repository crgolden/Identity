namespace Identity.Tests.E2E;

using Infrastructure;

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

            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
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

            // Should stay on login page with a validation error
            await page.WaitForURLAsync("**/Account/Login**");
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

                // Set up listener before click so the async form.submit() POST is captured race-free.
                var postResponse = page.WaitForResponseAsync(
                    res => res.Request.Method == "POST" && res.Url.Contains("/Account/Login"));
                await page.ClickAsync("#login-submit");
                await postResponse;

                // Locator polling avoids both WaitForLoadStateAsync premature-return and GotoAsync navigation conflicts.
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
