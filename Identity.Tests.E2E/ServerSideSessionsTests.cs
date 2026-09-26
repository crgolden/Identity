namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ServerSideSessionsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ServerSideSessions_AfterLogin_PageLoads()
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

            await page.GotoAsync(Pages.Account.Manage.ServerSideSessions.ServerSideSessionsPagePath);
            await page.WaitForURLAsync("**/Account/Manage/ServerSideSessions**");
            Assert.DoesNotContain(PageRoutes.Login, page.Url, StringComparison.Ordinal);
            Assert.DoesNotContain(PageRoutes.Error, page.Url, StringComparison.Ordinal);
        }
    }
}
