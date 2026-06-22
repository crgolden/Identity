namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ServerSideSessionsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ServerSideSessions_AfterLogin_PageLoads()
    {
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", fixture.SharedEmail);
            await page.FillAsync("input[name='Input.Password']", fixture.SharedPassword);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Account/Manage/ServerSideSessions");
            await page.WaitForURLAsync("**/Account/Manage/ServerSideSessions**");

            // Page should load successfully (not redirect to error or login)
            Assert.DoesNotContain("/Account/Login", page.Url);
            Assert.DoesNotContain("/Error", page.Url);
        }
    }
}
