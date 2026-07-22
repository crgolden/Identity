namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

// Same shape as IdentityProvidersTests.cs. Details/Edit/Delete use a route parameter ({id:int}) while
// Index/Create do not -- confirmed by reading each .cshtml's @page directive directly (inconsistent
// within this entity, unlike IdentityProviders which is query-string throughout). Tests navigate via
// clicking generated links (#btn-edit/#btn-delete) rather than constructing URLs manually, avoiding the
// need to parse the created entity's id out of the redirect URL.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class SamlServiceProvidersTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var entityId = $"e2e-sp-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlServiceProviders/Create");
            await page.FillAsync("#SamlServiceProvider_EntityId", entityId);
            await page.FillAsync("#SamlServiceProvider_DisplayName", "E2E Created SAML SP");
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_UpdatedValues_ShowInDetails()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var entityId = $"e2e-sp-edit-{Guid.NewGuid():N}";
        var updatedDisplayName = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateSamlServiceProviderAsync(page, entityId);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Edit"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.FillAsync("#SamlServiceProvider_DisplayName", updatedDisplayName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedDisplayName)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var entityId = $"e2e-sp-delete-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateSamlServiceProviderAsync(page, entityId);

            await page.ClickAsync("#btn-delete");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Delete");
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.GotoAsync("/Admin/SamlServiceProviders");
            await Assertions.Expect(page.GetByText(entityId)).Not.ToBeVisibleAsync();
        }
    }

    private static async Task CreateSamlServiceProviderAsync(IPage page, string entityId)
    {
        await page.GotoAsync("/Admin/SamlServiceProviders/Create");
        await page.FillAsync("#SamlServiceProvider_EntityId", entityId);
        await page.FillAsync("#SamlServiceProvider_DisplayName", "E2E SAML SP");
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
