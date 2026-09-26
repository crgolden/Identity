namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class IdentityResourcesTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Index_Lists_Seeded_Resource()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-identity-resource-{Guid.NewGuid():N}";
        var resourceId = await fixture.SeedIdentityResourceAsync(name);

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityResources");
            await Assertions.Expect(page.Locator($"#details-{resourceId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-identity-resource-create-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityResources/Create");
            await page.FillAsync("#Resource_Name", name);
            await page.FillAsync("#Resource_DisplayName", Generated.NewDisplayName());
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityResources/Details"));
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedIdentityResourceAsync($"e2e-identity-resource-delete-{Guid.NewGuid():N}");

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityResources");
            await page.ClickAsync($"#delete-{resourceId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await Assertions.Expect(page.Locator($"#delete-{resourceId}")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedIdentityResourceAsync($"e2e-ir-claimtypes-add-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/IdentityResources/Edit/ClaimTypes/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claimtype-0", claimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.ClaimTypes.DetailsPageName));
            var claimTypeRowId = await fixture.GetSingleAsync<IdentityResourceClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedIdentityResourceAsync($"e2e-ir-claimtypes-remove-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddIdentityResourceClaimTypeRowAsync(page, resourceId, claimType);
            var claimTypeRowId = await fixture.GetSingleAsync<IdentityResourceClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/IdentityResources/Edit/ClaimTypes/{resourceId}");
            await page.ClickAsync("#claimtype-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.ClaimTypes.DetailsPageName));
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task ClaimTypes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedIdentityResourceAsync($"e2e-ir-claimtypes-update-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedClaimType = $"e2e-claimtype-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddIdentityResourceClaimTypeRowAsync(page, resourceId, claimType);

            await page.GotoAsync($"/Admin/IdentityResources/Edit/ClaimTypes/{resourceId}");
            await page.FillAsync("#claimtype-0", updatedClaimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.ClaimTypes.DetailsPageName));
            var claimTypeRowId = await fixture.GetSingleAsync<IdentityResourceClaim, int>(c => c.Type == updatedClaimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-type-row-{claimTypeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedIdentityResourceAsync($"e2e-ir-props-add-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/IdentityResources/Edit/Properties/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#property-key-0", key);
            await page.FillAsync("#property-value-0", Generated.NewPropertyValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<IdentityResourceProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedIdentityResourceAsync($"e2e-ir-props-remove-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddIdentityResourcePropertyRowAsync(page, resourceId, key, Generated.NewPropertyValue());
            var propertyRowId = await fixture.GetSingleAsync<IdentityResourceProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/IdentityResources/Edit/Properties/{resourceId}");
            await page.ClickAsync("#property-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.Properties.DetailsPageName));
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Properties_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedIdentityResourceAsync($"e2e-ir-props-update-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddIdentityResourcePropertyRowAsync(page, resourceId, key, Generated.NewPropertyValue());

            await page.GotoAsync($"/Admin/IdentityResources/Edit/Properties/{resourceId}");
            await page.FillAsync("#property-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<IdentityResourceProperty, int>(p => p.Value == updatedValue, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    private static async Task AddIdentityResourceClaimTypeRowAsync(IPage page, int resourceId, string claimType)
    {
        await page.GotoAsync($"/Admin/IdentityResources/Edit/ClaimTypes/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claimtype-0", claimType);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.ClaimTypes.DetailsPageName));
    }

    private static async Task AddIdentityResourcePropertyRowAsync(IPage page, int resourceId, string key, string value)
    {
        await page.GotoAsync($"/Admin/IdentityResources/Edit/Properties/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#property-key-0", key);
        await page.FillAsync("#property-value-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.IdentityResources.Edit.Properties.DetailsPageName));
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
