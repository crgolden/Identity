namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

// Covers Admin-E2E-Guide.md's AR1-AR5 scenarios: Index lists seeded resource, Create -> Details, Delete,
// and the two collection sub-pages beyond Claims/Properties -- Scopes and Secrets. Routes use a route
// parameter ({id:int}), not a query string, e.g. /Admin/ApiResources/Edit/Scopes/{id} -- confirmed by
// reading the .cshtml @page directives directly (differs from Clients' collection pages, which bind id
// via query string). All selectors are #id per project convention; each test uses its own uniquely-named
// seeded resource with exactly one collection row, always at index 0.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ApiResourcesTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Index_Lists_Seeded_Resource()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-resource-{Guid.NewGuid():N}";
        await fixture.SeedApiResourceAsync(name);

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources");
            await Assertions.Expect(page.GetByText(name)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-resource-create-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources/Create");
            await page.FillAsync("#Resource_Name", name);
            await page.FillAsync("#Resource_DisplayName", "E2E Created API Resource");
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var name = $"e2e-api-resource-delete-{Guid.NewGuid():N}";
        var resourceId = await fixture.SeedApiResourceAsync(name);

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources");
            await page.ClickAsync($"#delete-{resourceId}");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Delete");
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator($"#delete-{resourceId}")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-scopes-add-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#scope-0", scope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Scopes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(scope)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-scopes-remove-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceScopeRowAsync(page, resourceId, scope);

            await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
            await page.ClickAsync("#scope-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Scopes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(scope)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-scopes-update-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";
        var updatedScope = $"e2e-scope-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceScopeRowAsync(page, resourceId, scope);

            await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
            await page.FillAsync("#scope-0", updatedScope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Scopes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedScope)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Add_Persists()
    {
        // Secrets are write-only once stored -- assert the Description that IS echoed back on Details,
        // never the raw Value.
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-secrets-add-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#secret-description-0", description);
            await page.FillAsync("#secret-value-0", "e2e-secret-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(description)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-secrets-remove-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceSecretRowAsync(page, resourceId, description);

            await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
            await page.ClickAsync("#secret-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(description)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-secrets-update-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";
        var updatedDescription = $"e2e-secret-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceSecretRowAsync(page, resourceId, description);

            await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
            await page.FillAsync("#secret-description-0", updatedDescription);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedDescription)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-props-add-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#property-key-0", key);
            await page.FillAsync("#property-value-0", "e2e-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(key)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-props-remove-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourcePropertyRowAsync(page, resourceId, key, "e2e-value");

            await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
            await page.ClickAsync("#property-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(key)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-props-update-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourcePropertyRowAsync(page, resourceId, key, "e2e-value");

            await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
            await page.FillAsync("#property-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedValue)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-claimtypes-add-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claimtype-0", claimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-claimtypes-remove-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceClaimTypeRowAsync(page, resourceId, claimType);

            await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
            await page.ClickAsync("#claimtype-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ClaimTypes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var resourceId = await fixture.SeedApiResourceAsync($"e2e-ar-claimtypes-update-{Guid.NewGuid():N}");
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedClaimType = $"e2e-claimtype-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddResourceClaimTypeRowAsync(page, resourceId, claimType);

            await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
            await page.FillAsync("#claimtype-0", updatedClaimType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedClaimType)).ToBeVisibleAsync();
        }
    }

    private static async Task AddResourceScopeRowAsync(IPage page, int resourceId, string scope)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/Scopes/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#scope-0", scope);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Scopes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddResourceSecretRowAsync(IPage page, int resourceId, string description)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/Secrets/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#secret-description-0", description);
        await page.FillAsync("#secret-value-0", "e2e-secret-value");
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddResourcePropertyRowAsync(IPage page, int resourceId, string key, string value)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/Properties/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#property-key-0", key);
        await page.FillAsync("#property-value-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddResourceClaimTypeRowAsync(IPage page, int resourceId, string claimType)
    {
        await page.GotoAsync($"/Admin/ApiResources/Edit/ClaimTypes/{resourceId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claimtype-0", claimType);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/ApiResources/Details/ClaimTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
