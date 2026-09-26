namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Identity.Pages.Admin;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AdminLandingTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Every_Card_Manage_Link_Navigates_To_Its_Index()
    {
        var sections = fixture.Factory.Services.GetRequiredService<IOptions<IReadOnlyList<AdminSection>>>().Value;
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            foreach (var section in sections)
            {
                await page.GotoAsync(AuthorizationNames.AdminFolder);
                await page.ClickAsync($"#{section.CardId}");
                await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
                Assert.Equal(section.Path, new Uri(page.Url).AbsolutePath);
            }
        }
    }

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(PageRoutes.Login);
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
    }
}
