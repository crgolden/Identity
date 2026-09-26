namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class DiagnosticsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Diagnostics_AuthenticatedUser_ShowsClaims()
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

            await page.GotoAsync("/Account/Manage/Diagnostics", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            await Assertions.Expect(page.Locator("#diagnostics-claims")).ToBeVisibleAsync();
            Assert.NotEqual(0, await page.Locator("[id^='diagnostics-claim-']").CountAsync());
        }
    }
}
