namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class SamlServiceProvidersTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var entityId = $"e2e-sp-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlServiceProviders/Create");
            await page.FillAsync("#SamlServiceProvider_EntityId", entityId);
            await page.FillAsync("#SamlServiceProvider_DisplayName", Generated.NewDisplayName());
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Details"));
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_UpdatedValues_ShowInDetails()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var entityId = $"e2e-sp-edit-{Guid.NewGuid():N}";
        var updatedDisplayName = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateSamlServiceProviderAsync(page, entityId);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Edit"));
            await page.FillAsync("#SamlServiceProvider_DisplayName", updatedDisplayName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Details"));
            await Assertions.Expect(page.Locator("#sp-display-name")).ToBeVisibleAsync();
            var persistedDisplayName = await fixture.GetSingleAsync<SamlServiceProvider, string>(p => p.EntityId == entityId, p => p.DisplayName);
            Assert.Equal(updatedDisplayName, persistedDisplayName);
        }
    }

    [Fact]
    public async Task Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var entityId = $"e2e-sp-delete-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateSamlServiceProviderAsync(page, entityId);
            var serviceProviderId = await fixture.GetSingleAsync<SamlServiceProvider, int>(p => p.EntityId == entityId, p => p.Id);
            await page.GotoAsync("/Admin/SamlServiceProviders");
            await Assertions.Expect(page.Locator($"#details-{serviceProviderId}")).ToBeVisibleAsync();
            await page.ClickAsync($"#details-{serviceProviderId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Details"));

            await page.ClickAsync("#btn-delete");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await page.GotoAsync("/Admin/SamlServiceProviders");
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator($"#details-{serviceProviderId}")).ToHaveCountAsync(0);
        }
    }

    private static async Task CreateSamlServiceProviderAsync(IPage page, string entityId)
    {
        await page.GotoAsync("/Admin/SamlServiceProviders/Create");
        await page.FillAsync("#SamlServiceProvider_EntityId", entityId);
        await page.FillAsync("#SamlServiceProvider_DisplayName", Generated.NewDisplayName());
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/SamlServiceProviders/Details"));
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
