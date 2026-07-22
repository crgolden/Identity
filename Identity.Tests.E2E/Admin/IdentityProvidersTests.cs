namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

// Covers Admin-E2E-Guide.md's IP2-IP4 scenarios (IP1 Index-loads already exists in AdminTests.cs).
// Flat-form CRUD only -- no collection sub-pages. Route is query-string based (@page, no {id:int}
// template) -- confirmed by reading Index/Create/Details/Edit/Delete.cshtml directly. No seed method
// needed: Create IS the row-producing step for these scenarios.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class IdentityProvidersTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scheme = $"e2e-idp-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityProviders/Create");
            await page.FillAsync("#IdentityProvider_Scheme", scheme);
            await page.FillAsync("#IdentityProvider_DisplayName", "E2E Created Identity Provider");
            await page.FillAsync("#IdentityProvider_Type", "oidc");
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_UpdatedValues_ShowInDetails()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scheme = $"e2e-idp-edit-{Guid.NewGuid():N}";
        var updatedDisplayName = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateIdentityProviderAsync(page, scheme);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Edit"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.FillAsync("#IdentityProvider_DisplayName", updatedDisplayName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedDisplayName)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scheme = $"e2e-idp-delete-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateIdentityProviderAsync(page, scheme);

            await page.ClickAsync("#btn-delete");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Delete");
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.GotoAsync("/Admin/IdentityProviders");
            await Assertions.Expect(page.GetByText(scheme)).Not.ToBeVisibleAsync();
        }
    }

    private static async Task CreateIdentityProviderAsync(IPage page, string scheme)
    {
        await page.GotoAsync("/Admin/IdentityProviders/Create");
        await page.FillAsync("#IdentityProvider_Scheme", scheme);
        await page.FillAsync("#IdentityProvider_DisplayName", "E2E Identity Provider");
        await page.FillAsync("#IdentityProvider_Type", "oidc");
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
