namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ApiResourcesTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Index_Lists_Seeded_Resource()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-resource-{Guid.NewGuid():N}";
        var resourceId = await fixture.SeedApiResourceAsync(name);

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources");
            await Assertions.Expect(page.Locator($"#details-{resourceId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-resource-create-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources/Create");
            await page.FillAsync("#Resource_Name", name);
            await page.FillAsync("#Resource_DisplayName", Generated.NewDisplayName());
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details"));
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-resource-delete-{Guid.NewGuid():N}";
        var resourceId = await fixture.SeedApiResourceAsync(name);

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources");
            await page.ClickAsync($"#delete-{resourceId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await Assertions.Expect(page.Locator($"#delete-{resourceId}")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-scopes-add-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#scope-0", scope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Scopes.DetailsPageName));
            var scopeRowId = await fixture.GetSingleAsync<ApiResourceScope, int>(s => s.Scope == scope, s => s.Id);
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-scopes-remove-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceScopeRowAsync(page, resourceId, scope);
            var scopeRowId = await fixture.GetSingleAsync<ApiResourceScope, int>(s => s.Scope == scope, s => s.Id);
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
            await page.ClickAsync("#scope-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Scopes.DetailsPageName));
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Scopes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-scopes-update-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";
        var updatedScope = $"e2e-scope-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceScopeRowAsync(page, resourceId, scope);

            await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
            await page.FillAsync("#scope-0", updatedScope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Scopes.DetailsPageName));
            var scopeRowId = await fixture.GetSingleAsync<ApiResourceScope, int>(s => s.Scope == updatedScope, s => s.Id);
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-secrets-add-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#secret-description-0", description);
            await page.FillAsync("#secret-value-0", Generated.NewSecretValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Secrets.DetailsPageName));
            var secretRowId = await fixture.GetSingleAsync<ApiResourceSecret, int>(s => s.Description == description, s => s.Id);
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-secrets-remove-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceSecretRowAsync(page, resourceId, description);
            var secretRowId = await fixture.GetSingleAsync<ApiResourceSecret, int>(s => s.Description == description, s => s.Id);
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
            await page.ClickAsync("#secret-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Secrets.DetailsPageName));
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Secrets_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-secrets-update-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";
        var updatedDescription = $"e2e-secret-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceSecretRowAsync(page, resourceId, description);

            await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
            await page.FillAsync("#secret-description-0", updatedDescription);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Secrets.DetailsPageName));
            var secretRowId = await fixture.GetSingleAsync<ApiResourceSecret, int>(s => s.Description == updatedDescription, s => s.Id);
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-props-add-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#property-key-0", key);
            await page.FillAsync("#property-value-0", Generated.NewPropertyValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<ApiResourceProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-props-remove-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourcePropertyRowAsync(page, resourceId, key, Generated.NewPropertyValue());
            var propertyRowId = await fixture.GetSingleAsync<ApiResourceProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
            await page.ClickAsync("#property-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Properties.DetailsPageName));
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Properties_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-props-update-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourcePropertyRowAsync(page, resourceId, key, Generated.NewPropertyValue());

            await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
            await page.FillAsync("#property-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<ApiResourceProperty, int>(p => p.Value == updatedValue, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-claimtypes-add-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claimtype-0", claimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.ClaimTypes.DetailsPageName));
            var claimTypeRowId = await fixture.GetSingleAsync<ApiResourceClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-claimtypes-remove-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceClaimTypeRowAsync(page, resourceId, claimType);
            var claimTypeRowId = await fixture.GetSingleAsync<ApiResourceClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
            await page.ClickAsync("#claimtype-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.ClaimTypes.DetailsPageName));
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task ClaimTypes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-claimtypes-update-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedClaimType = $"e2e-claimtype-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceClaimTypeRowAsync(page, resourceId, claimType);

            await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
            await page.FillAsync("#claimtype-0", updatedClaimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.ClaimTypes.DetailsPageName));
            var claimTypeRowId = await fixture.GetSingleAsync<ApiResourceClaim, int>(c => c.Type == updatedClaimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();
        }
    }

    private static async Task AddResourceScopeRowAsync(IPage page, int resourceId, string scope)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#scope-0", scope);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Scopes.DetailsPageName));
    }

    private static async Task AddResourceSecretRowAsync(IPage page, int resourceId, string description)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#secret-description-0", description);
        await page.FillAsync("#secret-value-0", Generated.NewSecretValue());
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Secrets.DetailsPageName));
    }

    private static async Task AddResourcePropertyRowAsync(IPage page, int resourceId, string key, string value)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#property-key-0", key);
        await page.FillAsync("#property-value-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.Properties.DetailsPageName));
    }

    private static async Task AddResourceClaimTypeRowAsync(IPage page, int resourceId, string claimType)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claimtype-0", claimType);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.ApiResources.Edit.ClaimTypes.DetailsPageName));
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
