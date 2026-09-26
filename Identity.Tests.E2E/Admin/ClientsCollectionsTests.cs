namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ClientsCollectionsTests(PlaywrightFixture fixture)
{
    private static readonly ClientCollection CorsOrigins = new(nameof(CorsOrigins));

    private static readonly ClientCollection GrantTypes = new(nameof(GrantTypes));

    private static readonly ClientCollection IdPRestrictions = new(nameof(IdPRestrictions));

    private static readonly ClientCollection PostLogoutRedirectUris = new(nameof(PostLogoutRedirectUris));

    private static readonly ClientCollection RedirectUris = new(nameof(RedirectUris));

    private static readonly ClientCollection Scopes = new(nameof(Scopes));

    [Fact]
    public async Task Claims_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-claims-add-{Guid.NewGuid():N}");
        var claimType = $"e2e-type-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claim-type-0", claimType);
            await page.FillAsync("#claim-value-0", Generated.NewPropertyValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Claims.DetailsPageName));
            var claimRowId = await fixture.GetSingleAsync<ClientClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-row-{claimRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Claims_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-claims-remove-{Guid.NewGuid():N}");
        var claimType = $"e2e-type-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddClaimRowAsync(page, clientDbId, claimType);
            var claimRowId = await fixture.GetSingleAsync<ClientClaim, int>(c => c.Type == claimType, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-row-{claimRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
            await page.ClickAsync("#claim-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator($"#claim-row-{claimRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Claims_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-claims-update-{Guid.NewGuid():N}");
        var claimType = $"e2e-type-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddClaimRowAsync(page, clientDbId, claimType);

            await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
            await page.FillAsync("#claim-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Claims.DetailsPageName));
            var claimRowId = await fixture.GetSingleAsync<ClientClaim, int>(c => c.Value == updatedValue, c => c.Id);
            await Assertions.Expect(page.Locator($"#claim-row-{claimRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task CorsOrigins_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-cors-add-{Guid.NewGuid():N}");
        var origin = $"https://e2e-{Guid.NewGuid():N}.test";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/CorsOrigins?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#corsorigin-0", origin);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.CorsOrigins.DetailsPageName));
            var originRowId = await fixture.GetSingleAsync<ClientCorsOrigin, int>(o => o.Origin == origin, o => o.Id);
            await Assertions.Expect(page.Locator($"#cors-origin-row-{originRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task CorsOrigins_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-cors-remove-{Guid.NewGuid():N}");
        var origin = $"https://e2e-{Guid.NewGuid():N}.test";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, CorsOrigins, clientDbId, origin);
            var originRowId = await fixture.GetSingleAsync<ClientCorsOrigin, int>(o => o.Origin == origin, o => o.Id);
            await Assertions.Expect(page.Locator($"#cors-origin-row-{originRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/CorsOrigins?id={clientDbId}");
            await page.ClickAsync("#corsorigin-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.CorsOrigins.DetailsPageName));
            await Assertions.Expect(page.Locator($"#cors-origin-row-{originRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task CorsOrigins_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-cors-update-{Guid.NewGuid():N}");
        var origin = $"https://e2e-{Guid.NewGuid():N}.test";
        var updatedOrigin = $"https://e2e-updated-{Guid.NewGuid():N}.test";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, CorsOrigins, clientDbId, origin);

            await page.GotoAsync($"/Admin/Clients/Edit/CorsOrigins?id={clientDbId}");
            await page.FillAsync("#corsorigin-0", updatedOrigin);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.CorsOrigins.DetailsPageName));
            var originRowId = await fixture.GetSingleAsync<ClientCorsOrigin, int>(o => o.Origin == updatedOrigin, o => o.Id);
            await Assertions.Expect(page.Locator($"#cors-origin-row-{originRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task GrantTypes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-granttypes-add-{Guid.NewGuid():N}");
        var grantType = $"e2e-grant-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/GrantTypes?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#granttype-0", grantType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.GrantTypes.DetailsPageName));
            var grantTypeRowId = await fixture.GetSingleAsync<ClientGrantType, int>(g => g.GrantType == grantType, g => g.Id);
            await Assertions.Expect(page.Locator($"#grant-type-row-{grantTypeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task GrantTypes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-granttypes-remove-{Guid.NewGuid():N}");
        var grantType = $"e2e-grant-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, GrantTypes, clientDbId, grantType);
            var grantTypeRowId = await fixture.GetSingleAsync<ClientGrantType, int>(g => g.GrantType == grantType, g => g.Id);
            await Assertions.Expect(page.Locator($"#grant-type-row-{grantTypeRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/GrantTypes?id={clientDbId}");
            await page.ClickAsync("#granttype-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.GrantTypes.DetailsPageName));
            await Assertions.Expect(page.Locator($"#grant-type-row-{grantTypeRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task GrantTypes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-granttypes-update-{Guid.NewGuid():N}");
        var grantType = $"e2e-grant-{Guid.NewGuid():N}";
        var updatedGrantType = $"e2e-grant-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, GrantTypes, clientDbId, grantType);

            await page.GotoAsync($"/Admin/Clients/Edit/GrantTypes?id={clientDbId}");
            await page.FillAsync("#granttype-0", updatedGrantType);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.GrantTypes.DetailsPageName));
            var grantTypeRowId = await fixture.GetSingleAsync<ClientGrantType, int>(g => g.GrantType == updatedGrantType, g => g.Id);
            await Assertions.Expect(page.Locator($"#grant-type-row-{grantTypeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task IdPRestrictions_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-idp-add-{Guid.NewGuid():N}");
        var provider = $"e2e-provider-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/IdPRestrictions?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#idprestriction-0", provider);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.IdPRestrictions.DetailsPageName));
            var restrictionRowId = await fixture.GetSingleAsync<ClientIdPRestriction, int>(r => r.Provider == provider, r => r.Id);
            await Assertions.Expect(page.Locator($"#idp-restriction-row-{restrictionRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task IdPRestrictions_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-idp-remove-{Guid.NewGuid():N}");
        var provider = $"e2e-provider-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, IdPRestrictions, clientDbId, provider);
            var restrictionRowId = await fixture.GetSingleAsync<ClientIdPRestriction, int>(r => r.Provider == provider, r => r.Id);
            await Assertions.Expect(page.Locator($"#idp-restriction-row-{restrictionRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/IdPRestrictions?id={clientDbId}");
            await page.ClickAsync("#idprestriction-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.IdPRestrictions.DetailsPageName));
            await Assertions.Expect(page.Locator($"#idp-restriction-row-{restrictionRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task IdPRestrictions_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-idp-update-{Guid.NewGuid():N}");
        var provider = $"e2e-provider-{Guid.NewGuid():N}";
        var updatedProvider = $"e2e-provider-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, IdPRestrictions, clientDbId, provider);

            await page.GotoAsync($"/Admin/Clients/Edit/IdPRestrictions?id={clientDbId}");
            await page.FillAsync("#idprestriction-0", updatedProvider);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.IdPRestrictions.DetailsPageName));
            var restrictionRowId = await fixture.GetSingleAsync<ClientIdPRestriction, int>(r => r.Provider == updatedProvider, r => r.Id);
            await Assertions.Expect(page.Locator($"#idp-restriction-row-{restrictionRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task PostLogoutRedirectUris_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-plru-add-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/logout";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/PostLogoutRedirectUris?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#postlogoutredirecturi-0", uri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.PostLogoutRedirectUris.DetailsPageName));
            var uriRowId = await fixture.GetSingleAsync<ClientPostLogoutRedirectUri, int>(u => u.PostLogoutRedirectUri == uri, u => u.Id);
            await Assertions.Expect(page.Locator($"#post-logout-redirect-uri-row-{uriRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task PostLogoutRedirectUris_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-plru-remove-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/logout";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, PostLogoutRedirectUris, clientDbId, uri);
            var uriRowId = await fixture.GetSingleAsync<ClientPostLogoutRedirectUri, int>(u => u.PostLogoutRedirectUri == uri, u => u.Id);
            await Assertions.Expect(page.Locator($"#post-logout-redirect-uri-row-{uriRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/PostLogoutRedirectUris?id={clientDbId}");
            await page.ClickAsync("#postlogoutredirecturi-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.PostLogoutRedirectUris.DetailsPageName));
            await Assertions.Expect(page.Locator($"#post-logout-redirect-uri-row-{uriRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task PostLogoutRedirectUris_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-plru-update-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/logout";
        var updatedUri = $"https://e2e-updated-{Guid.NewGuid():N}.test/logout";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, PostLogoutRedirectUris, clientDbId, uri);

            await page.GotoAsync($"/Admin/Clients/Edit/PostLogoutRedirectUris?id={clientDbId}");
            await page.FillAsync("#postlogoutredirecturi-0", updatedUri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.PostLogoutRedirectUris.DetailsPageName));
            var uriRowId = await fixture.GetSingleAsync<ClientPostLogoutRedirectUri, int>(u => u.PostLogoutRedirectUri == updatedUri, u => u.Id);
            await Assertions.Expect(page.Locator($"#post-logout-redirect-uri-row-{uriRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-props-add-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#property-key-0", key);
            await page.FillAsync("#property-value-0", Generated.NewPropertyValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<ClientProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Properties_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-props-remove-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddPropertyRowAsync(page, clientDbId, key, Generated.NewPropertyValue());
            var propertyRowId = await fixture.GetSingleAsync<ClientProperty, int>(p => p.Key == key, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
            await page.ClickAsync("#property-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Properties.DetailsPageName));
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Properties_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-props-update-{Guid.NewGuid():N}");
        var key = $"e2e-key-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddPropertyRowAsync(page, clientDbId, key, Generated.NewPropertyValue());

            await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
            await page.FillAsync("#property-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Properties.DetailsPageName));
            var propertyRowId = await fixture.GetSingleAsync<ClientProperty, int>(p => p.Value == updatedValue, p => p.Id);
            await Assertions.Expect(page.Locator($"#property-row-{propertyRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task RedirectUris_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-redirect-add-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/callback";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/RedirectUris?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#redirecturi-0", uri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.RedirectUris.DetailsPageName));
            var uriRowId = await fixture.GetSingleAsync<ClientRedirectUri, int>(u => u.RedirectUri == uri, u => u.Id);
            await Assertions.Expect(page.Locator($"#redirect-uri-row-{uriRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task RedirectUris_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-redirect-remove-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/callback";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, RedirectUris, clientDbId, uri);
            var uriRowId = await fixture.GetSingleAsync<ClientRedirectUri, int>(u => u.RedirectUri == uri, u => u.Id);
            await Assertions.Expect(page.Locator($"#redirect-uri-row-{uriRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/RedirectUris?id={clientDbId}");
            await page.ClickAsync("#redirecturi-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.RedirectUris.DetailsPageName));
            await Assertions.Expect(page.Locator($"#redirect-uri-row-{uriRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task RedirectUris_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-redirect-update-{Guid.NewGuid():N}");
        var uri = $"https://e2e-{Guid.NewGuid():N}.test/callback";
        var updatedUri = $"https://e2e-updated-{Guid.NewGuid():N}.test/callback";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, RedirectUris, clientDbId, uri);

            await page.GotoAsync($"/Admin/Clients/Edit/RedirectUris?id={clientDbId}");
            await page.FillAsync("#redirecturi-0", updatedUri);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.RedirectUris.DetailsPageName));
            var uriRowId = await fixture.GetSingleAsync<ClientRedirectUri, int>(u => u.RedirectUri == updatedUri, u => u.Id);
            await Assertions.Expect(page.Locator($"#redirect-uri-row-{uriRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-scopes-add-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Scopes?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#scope-0", scope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Scopes.DetailsPageName));
            var scopeRowId = await fixture.GetSingleAsync<ClientScope, int>(s => s.Scope == scope, s => s.Id);
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Scopes_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-scopes-remove-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, Scopes, clientDbId, scope);
            var scopeRowId = await fixture.GetSingleAsync<ClientScope, int>(s => s.Scope == scope, s => s.Id);
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/Scopes?id={clientDbId}");
            await page.ClickAsync("#scope-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Scopes.DetailsPageName));
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Scopes_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-scopes-update-{Guid.NewGuid():N}");
        var scope = $"e2e-scope-{Guid.NewGuid():N}";
        var updatedScope = $"e2e-scope-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSingleFieldRowAsync(page, Scopes, clientDbId, scope);

            await page.GotoAsync($"/Admin/Clients/Edit/Scopes?id={clientDbId}");
            await page.FillAsync("#scope-0", updatedScope);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Scopes.DetailsPageName));
            var scopeRowId = await fixture.GetSingleAsync<ClientScope, int>(s => s.Scope == updatedScope, s => s.Id);
            await Assertions.Expect(page.Locator($"#scope-row-{scopeRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-secrets-add-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#secret-description-0", description);
            await page.FillAsync("#secret-value-0", Generated.NewSecretValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Secrets.DetailsPageName));
            var secretRowId = await fixture.GetSingleAsync<ClientSecret, int>(s => s.Description == description, s => s.Id);
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Secrets_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-secrets-remove-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSecretRowAsync(page, clientDbId, description);
            var secretRowId = await fixture.GetSingleAsync<ClientSecret, int>(s => s.Description == description, s => s.Id);
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToBeVisibleAsync();

            await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
            await page.ClickAsync("#secret-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Secrets.DetailsPageName));
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Secrets_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync($"e2e-secrets-update-{Guid.NewGuid():N}");
        var description = $"e2e-secret-{Guid.NewGuid():N}";
        var updatedDescription = $"e2e-secret-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await AddSecretRowAsync(page, clientDbId, description);

            await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
            await page.FillAsync("#secret-description-0", updatedDescription);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Secrets.DetailsPageName));
            var secretRowId = await fixture.GetSingleAsync<ClientSecret, int>(s => s.Description == updatedDescription, s => s.Id);
            await Assertions.Expect(page.Locator($"#secret-row-{secretRowId}")).ToBeVisibleAsync();
        }
    }

    private static async Task AddClaimRowAsync(IPage page, int clientDbId, string claimType)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/Claims?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claim-type-0", claimType);
        await page.FillAsync("#claim-value-0", Generated.NewPropertyValue());
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Claims.DetailsPageName));
    }

    private static async Task AddPropertyRowAsync(IPage page, int clientDbId, string key, string value)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/Properties?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#property-key-0", key);
        await page.FillAsync("#property-value-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Properties.DetailsPageName));
    }

    private static async Task AddSecretRowAsync(IPage page, int clientDbId, string description)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/Secrets?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#secret-description-0", description);
        await page.FillAsync("#secret-value-0", Generated.NewSecretValue());
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Clients.Edit.Secrets.DetailsPageName));
    }

    private static async Task AddSingleFieldRowAsync(
        IPage page, ClientCollection collection, int clientDbId, string value)
    {
        await page.GotoAsync($"/Admin/Clients/Edit/{collection.Page}?id={clientDbId}");
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync($"#{collection.IdPrefix}-0", value);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"/Admin/Clients/Details/{collection.Page}"));
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
