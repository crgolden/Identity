namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

// Covers Admin-E2E-Guide.md's AS1-AS3 scenarios: standard CRUD plus the ClaimTypes and Properties
// collection sub-pages (shared _EditClaimTypesForm.cshtml / _EditPropertiesForm.cshtml partials, same
// as ApiResources/IdentityResources). Route uses {id:int}, not a query string. All selectors are #id.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ApiScopesTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Index_Lists_Seeded_Scope()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-scope-{Guid.NewGuid():N}";
        await fixture.SeedApiScopeAsync(name);

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes");
            await Assertions.Expect(page.GetByText(name)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-scope-create-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes/Create");
            await page.FillAsync("#Scope_Name", name);
            await page.FillAsync("#Scope_DisplayName", "E2E Created API Scope");
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-api-scope-delete-{Guid.NewGuid():N}");

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes");
            await page.ClickAsync($"#delete-{scopeId}");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Delete");
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator($"#delete-{scopeId}")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-claimtypes-add-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claimtype-0", claimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-claimtypes-remove-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopeClaimTypeRowAsync(page, scopeId, claimType);

            await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
            await page.ClickAsync("#claimtype-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-claimtypes-update-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedClaimType = $"e2e-claimtype-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopeClaimTypeRowAsync(page, scopeId, claimType);

            await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
            await page.FillAsync("#claimtype-0", updatedClaimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedClaimType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-props-add-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#property-key-0", key);
            await page.FillAsync("#property-value-0", "e2e-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(key)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-props-remove-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopePropertyRowAsync(page, scopeId, key, "e2e-value");

            await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
            await page.ClickAsync("#property-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(key)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scopeId = await fixture.SeedApiScopeAsync($"e2e-as-props-update-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddScopePropertyRowAsync(page, scopeId, key, "e2e-value");

            await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
            await page.FillAsync("#property-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedValue)).ToBeVisibleAsync();
        }
    }

    private static async Task AddScopeClaimTypeRowAsync(IPage page, int scopeId, string claimType)
    {
        await page.GotoAsync($"/Admin/ApiScopes/Edit/ClaimTypes/{scopeId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claimtype-0", claimType);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddScopePropertyRowAsync(IPage page, int scopeId, string key, string value)
    {
        await page.GotoAsync($"/Admin/ApiScopes/Edit/Properties/{scopeId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#property-key-0", key);
        await page.FillAsync("#property-value-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiScopes/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
