namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ApiScopesTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Index_Lists_Seeded_Scope()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-scope-{Guid.NewGuid():N}";
        var scopeId = await fixture.SeedApiScopeAsync(name);

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes");
            await Assertions.Expect(page.Locator($"#details-{scopeId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-scope-create-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes/Create");
            await page.FillAsync("#Scope_Name", name);
            await page.FillAsync("#Scope_DisplayName", Generated.NewDisplayName());
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details"));
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-api-scope-delete-{Guid.NewGuid():N}");

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes");
            await page.ClickAsync($"#delete-{scopeId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await Assertions.Expect(page.Locator($"#delete-{scopeId}")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-claimtypes-add-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claimtype-0", claimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.ClaimTypes.DetailsPageName));
            var claimTypeRowId = await fixture.GetSingleAsync<ApiScopeClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-claimtypes-remove-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopeClaimTypeRowAsync(page, scopeId, claimType);
            var claimTypeRowId = await fixture.GetSingleAsync<ApiScopeClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
            await page.ClickAsync("#claimtype-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.ClaimTypes.DetailsPageName));
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task ClaimTypes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-claimtypes-update-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedClaimType = $"e2e-claimtype-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopeClaimTypeRowAsync(page, scopeId, claimType);

            await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
            await page.FillAsync("#claimtype-0", updatedClaimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.ClaimTypes.DetailsPageName));
            var claimTypeRowId = await fixture.GetSingleAsync<ApiScopeClaim, int>(c => c.Type == updatedClaimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-props-add-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#property-key-0", key);
            await page.FillAsync("#property-value-0", Generated.NewPropertyValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<ApiScopeProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-props-remove-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopePropertyRowAsync(page, scopeId, key, Generated.NewPropertyValue());
            var propertyRowId = await fixture.GetSingleAsync<ApiScopeProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
            await page.ClickAsync("#property-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.Properties.DetailsPageName));
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Properties_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-props-update-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopePropertyRowAsync(page, scopeId, key, Generated.NewPropertyValue());

            await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
            await page.FillAsync("#property-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<ApiScopeProperty, int>(p => p.Value == updatedValue, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    private static async Task AddScopeClaimTypeRowAsync(IPage page, int scopeId, string claimType)
    {
        await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claimtype-0", claimType);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.ClaimTypes.DetailsPageName));
    }

    private static async Task AddScopePropertyRowAsync(IPage page, int scopeId, string key, string value)
    {
        await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#property-key-0", key);
        await page.FillAsync("#property-value-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiScopes.Edit.Properties.DetailsPageName));
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
