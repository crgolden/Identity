namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class GrantsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Grants_AuthenticatedUser_PageLoads()
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

            await page.GotoAsync("/Account/Manage/Grants");
            await page.WaitForURLAsync("**/Account/Manage/Grants**");
            Assert.DoesNotContain("/Account/Login", page.Url);
            Assert.DoesNotContain("/Error", page.Url);
        }
    }
}