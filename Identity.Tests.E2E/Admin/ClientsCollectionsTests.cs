namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

// Covers Admin-E2E-Guide.md's C6-C9 scenarios (repeated across all 9 Client collection sub-pages):
// add a row -> save -> persists; remove a row -> save -> persists; update an existing row -> save ->
// persists. Every collection row field and remove button now carries a stable id (index-based --
// "{field}-{i}"), added to the Razor views and admin-collection.js specifically to support this: per
// project convention, E2E tests select elements by id only, never by aria-label/attribute/text
// selectors. Each test uses its own freshly-seeded client with exactly one row, so that row is always
// at index 0.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ClientsCollectionsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Claims_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-claims-add-{Guid.NewGuid():N}");
        var claimType = $"e2e-type-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claim-type-0", claimType);
            await page.FillAsync("#claim-value-0", "e2e-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Claims_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-claims-remove-{Guid.NewGuid():N}");
        var claimType = $"e2e-type-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddClaimRowAsync(page, clientDbId, claimType);

            await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
            await page.ClickAsync("#claim-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Claims_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-claims-update-{Guid.NewGuid():N}");
        var claimType = $"e2e-type-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddClaimRowAsync(page, clientDbId, claimType);

            await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
            await page.FillAsync("#claim-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedValue)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task CorsOrigins_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-cors-add-{Guid.NewGuid():N}");
        var origin = $"https://e2e-{Guid.NewGuid():N}.test";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/CorsOrigins?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#corsorigin-0", origin);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/CorsOrigins"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(origin)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task CorsOrigins_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-cors-remove-{Guid.NewGuid():N}");
        var origin = $"https://e2e-{Guid.NewGuid():N}.test";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "CorsOrigins", clientDbId, "corsorigin", origin);

            await page.GotoAsync($"/Admin/Clients/Edit/CorsOrigins?id={clientDbId}");
            await page.ClickAsync("#corsorigin-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/CorsOrigins"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(origin)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task CorsOrigins_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-cors-update-{Guid.NewGuid():N}");
        var origin = $"https://e2e-{Guid.NewGuid():N}.test";
        var updatedOrigin = $"https://e2e-updated-{Guid.NewGuid():N}.test";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "CorsOrigins", clientDbId, "corsorigin", origin);

            await page.GotoAsync($"/Admin/Clients/Edit/CorsOrigins?id={clientDbId}");
            await page.FillAsync("#corsorigin-0", updatedOrigin);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/CorsOrigins"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedOrigin)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task GrantTypes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-granttypes-add-{Guid.NewGuid():N}");
        var grantType = $"e2e-grant-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/GrantTypes?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#granttype-0", grantType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/GrantTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(grantType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task GrantTypes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-granttypes-remove-{Guid.NewGuid():N}");
        var grantType = $"e2e-grant-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "GrantTypes", clientDbId, "granttype", grantType);

            await page.GotoAsync($"/Admin/Clients/Edit/GrantTypes?id={clientDbId}");
            await page.ClickAsync("#granttype-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/GrantTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(grantType)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task GrantTypes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-granttypes-update-{Guid.NewGuid():N}");
        var grantType = $"e2e-grant-{Guid.NewGuid():N}";
        var updatedGrantType = $"e2e-grant-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "GrantTypes", clientDbId, "granttype", grantType);

            await page.GotoAsync($"/Admin/Clients/Edit/GrantTypes?id={clientDbId}");
            await page.FillAsync("#granttype-0", updatedGrantType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/GrantTypes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedGrantType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task IdPRestrictions_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-idp-add-{Guid.NewGuid():N}");
        var provider = $"e2e-provider-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/IdPRestrictions?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#idprestriction-0", provider);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/IdPRestrictions"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(provider)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task IdPRestrictions_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-idp-remove-{Guid.NewGuid():N}");
        var provider = $"e2e-provider-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "IdPRestrictions", clientDbId, "idprestriction", provider);

            await page.GotoAsync($"/Admin/Clients/Edit/IdPRestrictions?id={clientDbId}");
            await page.ClickAsync("#idprestriction-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/IdPRestrictions"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(provider)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task IdPRestrictions_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-idp-update-{Guid.NewGuid():N}");
        var provider = $"e2e-provider-{Guid.NewGuid():N}";
        var updatedProvider = $"e2e-provider-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "IdPRestrictions", clientDbId, "idprestriction", provider);

            await page.GotoAsync($"/Admin/Clients/Edit/IdPRestrictions?id={clientDbId}");
            await page.FillAsync("#idprestriction-0", updatedProvider);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/IdPRestrictions"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedProvider)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task PostLogoutRedirectUris_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-plru-add-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/logout";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/PostLogoutRedirectUris?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#postlogoutredirecturi-0", uri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/PostLogoutRedirectUris"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(uri)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task PostLogoutRedirectUris_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-plru-remove-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/logout";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "PostLogoutRedirectUris", clientDbId, "postlogoutredirecturi", uri);

            await page.GotoAsync($"/Admin/Clients/Edit/PostLogoutRedirectUris?id={clientDbId}");
            await page.ClickAsync("#postlogoutredirecturi-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/PostLogoutRedirectUris"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(uri)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task PostLogoutRedirectUris_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-plru-update-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/logout";
        var updatedUri = $"https://e2e-updated-{Guid.NewGuid():N}.test/logout";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "PostLogoutRedirectUris", clientDbId, "postlogoutredirecturi", uri);

            await page.GotoAsync($"/Admin/Clients/Edit/PostLogoutRedirectUris?id={clientDbId}");
            await page.FillAsync("#postlogoutredirecturi-0", updatedUri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/PostLogoutRedirectUris"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedUri)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-props-add-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#property-key-0", key);
            await page.FillAsync("#property-value-0", "e2e-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(key)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-props-remove-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddPropertyRowAsync(page, clientDbId, key, "e2e-value");

            await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
            await page.ClickAsync("#property-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(key)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-props-update-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddPropertyRowAsync(page, clientDbId, key, "e2e-value");

            await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
            await page.FillAsync("#property-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedValue)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task RedirectUris_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-redirect-add-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/callback";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/RedirectUris?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#redirecturi-0", uri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/RedirectUris"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(uri)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task RedirectUris_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-redirect-remove-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/callback";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "RedirectUris", clientDbId, "redirecturi", uri);

            await page.GotoAsync($"/Admin/Clients/Edit/RedirectUris?id={clientDbId}");
            await page.ClickAsync("#redirecturi-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/RedirectUris"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(uri)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task RedirectUris_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-redirect-update-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/callback";
        var updatedUri = $"https://e2e-updated-{Guid.NewGuid():N}.test/callback";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "RedirectUris", clientDbId, "redirecturi", uri);

            await page.GotoAsync($"/Admin/Clients/Edit/RedirectUris?id={clientDbId}");
            await page.FillAsync("#redirecturi-0", updatedUri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/RedirectUris"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedUri)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-scopes-add-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Scopes?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#scope-0", scope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Scopes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(scope)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-scopes-remove-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "Scopes", clientDbId, "scope", scope);

            await page.GotoAsync($"/Admin/Clients/Edit/Scopes?id={clientDbId}");
            await page.ClickAsync("#scope-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Scopes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(scope)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-scopes-update-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";
        var updatedScope = $"e2e-scope-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, "Scopes", clientDbId, "scope", scope);

            await page.GotoAsync($"/Admin/Clients/Edit/Scopes?id={clientDbId}");
            await page.FillAsync("#scope-0", updatedScope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Scopes"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedScope)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Add_Persists()
    {
        // Secrets are write-only once stored (per Admin-E2E-Guide.md's own caveat) -- assert the
        // Description/Type that ARE echoed back on Details, never the raw Value.
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-secrets-add-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#secret-description-0", description);
            await page.FillAsync("#secret-value-0", "e2e-secret-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(description)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-secrets-remove-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSecretRowAsync(page, clientDbId, description);

            await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
            await page.ClickAsync("#secret-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(description)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-secrets-update-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";
        var updatedDescription = $"e2e-secret-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSecretRowAsync(page, clientDbId, description);

            await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
            await page.FillAsync("#secret-description-0", updatedDescription);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedDescription)).ToBeVisibleAsync();
        }
    }

    private static async Task AddClaimRowAsync(IPage page, int clientDbId, string claimType)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claim-type-0", claimType);
        await page.FillAsync("#claim-value-0", "e2e-value");
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddPropertyRowAsync(IPage page, int clientDbId, string key, string value)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#property-key-0", key);
        await page.FillAsync("#property-value-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Properties"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddSecretRowAsync(IPage page, int clientDbId, string description)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#secret-description-0", description);
        await page.FillAsync("#secret-value-0", "e2e-secret-value");
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details/Secrets"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddSingleFieldRowAsync(IPage page, string collection, int clientDbId, string idPrefix, string value)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/{collection}?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync($"#{idPrefix}-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/Admin/Clients/Details/{collection}"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
