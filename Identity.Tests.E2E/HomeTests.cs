namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class HomeTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Home_Anonymous_Shows_Register_And_SignIn_Calls_To_Action()
    {
        var (context, page) = await fixture.NewPageAsync("Home");
        await using (context)
        {
            await page.GotoAsync("/");

            await Assertions.Expect(page.Locator("#create-account")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#sign-in")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#manage-account")).Not.ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#review-grants")).Not.ToBeVisibleAsync();
            await Assertions.Expect(page).ToHaveTitleAsync("Home - Identity");
        }
    }

    [Fact]
    public async Task Home_SignedIn_Shows_Account_Links_Instead_Of_Register_And_SignIn()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync("Home");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/");

            await Assertions.Expect(page.Locator("#manage-account")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#review-grants")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#create-account")).Not.ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#sign-in")).Not.ToBeVisibleAsync();
            await Assertions.Expect(page).ToHaveTitleAsync("Home - Identity");
        }
    }

    [Fact]
    public async Task Home_SignedIn_Body_Agrees_With_Navbar()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync("Home");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/");

            await Assertions.Expect(page.Locator("#signed-in-lead")).ToContainTextAsync(email);
        }
    }

    [Fact]
    public async Task Home_AdminLink_Visible_When_AdminRole()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Home");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/");

            await Assertions.Expect(page.Locator("#home-admin")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Home_AdminLink_Hidden_When_NonAdminRole()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync("Home");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/");

            await Assertions.Expect(page.Locator("#home-admin")).Not.ToBeVisibleAsync();
        }
    }

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/Account/Login");
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("/Account/Login"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }
}