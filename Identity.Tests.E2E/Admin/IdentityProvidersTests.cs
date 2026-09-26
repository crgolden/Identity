namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class IdentityProvidersTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scheme = $"e2e-idp-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityProviders/Create");
            await page.FillAsync("#IdentityProvider_Scheme", scheme);
            await page.FillAsync("#IdentityProvider_DisplayName", Generated.NewDisplayName());
            await page.FillAsync("#IdentityProvider_Type", OidcStandardConstants.OidcProtocol);
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Details"));
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_UpdatedValues_ShowInDetails()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scheme = $"e2e-idp-edit-{Guid.NewGuid():N}";
        var updatedDisplayName = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateIdentityProviderAsync(page, scheme);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Edit"));
            await page.FillAsync("#IdentityProvider_DisplayName", updatedDisplayName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Details"));
            await Assertions.Expect(page.Locator("#idp-display-name")).ToBeVisibleAsync();
            var persistedDisplayName = await fixture.GetSingleAsync<IdentityProvider, string>(p => p.Scheme == scheme, p => p.DisplayName);
            Assert.Equal(updatedDisplayName, persistedDisplayName);
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var scheme = $"e2e-idp-delete-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateIdentityProviderAsync(page, scheme);
            var providerId = await fixture.GetSingleAsync<IdentityProvider, int>(p => p.Scheme == scheme, p => p.Id);
            await page.GotoAsync("/Admin/IdentityProviders");
            await Assertions.Expect(page.Locator($"#details-{providerId}")).ToBeVisibleAsync();
            await page.ClickAsync($"#details-{providerId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Details"));

            await page.ClickAsync("#btn-delete");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await page.GotoAsync("/Admin/IdentityProviders");
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator($"#details-{providerId}")).ToHaveCountAsync(0);
        }
    }

    private static async Task CreateIdentityProviderAsync(IPage page, string scheme)
    {
        await page.GotoAsync("/Admin/IdentityProviders/Create");
        await page.FillAsync("#IdentityProvider_Scheme", scheme);
        await page.FillAsync("#IdentityProvider_DisplayName", Generated.NewDisplayName());
        await page.FillAsync("#IdentityProvider_Type", OidcStandardConstants.OidcProtocol);
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/IdentityProviders/Details"));
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
