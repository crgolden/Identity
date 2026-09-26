namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Pages.Admin;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AdminTests(PlaywrightFixture fixture)
{
    private const string AdminCardSelector = "[id^='admin-card-']";

    [Fact]
    public async Task Admin_Nav_Link_Visible_When_AdminRole()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await Assertions.Expect(page.Locator("#admin-nav")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Nav_Link_Hidden_When_NonAdminRole()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await Assertions.Expect(page.Locator("#admin-nav")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Landing_Page_Shows_All_Section_Cards()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(AuthorizationNames.AdminFolder);

            await Assertions.Expect(page.Locator(AdminCardSelector))
                .ToHaveCountAsync(fixture.Settings.IndependentlyPinnedAdminCardCount);
            var sections = fixture.Factory.Services.GetRequiredService<IOptions<IReadOnlyList<AdminSection>>>().Value;
            Assert.Equal(fixture.Settings.IndependentlyPinnedAdminCardCount, sections.Count);

            foreach (var section in sections)
            {
                await Assertions.Expect(page.Locator($"#{section.CardId}")).ToBeVisibleAsync();
            }
        }
    }

    [Fact]
    public async Task Admin_Unauthenticated_Redirects_To_Login()
    {
        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await page.GotoAsync(AuthorizationNames.AdminFolder);
            Assert.Equal(PageRoutes.Login, new Uri(page.Url).AbsolutePath);
        }
    }

    [Fact]
    public async Task Manage_Unauthenticated_Redirects_To_Login()
    {
        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await page.GotoAsync("/Account/Manage");
            Assert.Equal(PageRoutes.Login, new Uri(page.Url).AbsolutePath);
        }
    }

    [Fact]
    public async Task Admin_NonAdminRole_Redirects_To_AccessDenied()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync(AuthorizationNames.AdminFolder);
            Assert.Equal("/Account/AccessDenied", new Uri(page.Url).AbsolutePath);
        }
    }

    [Fact]
    public async Task Admin_Clients_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Clients");
            Assert.Equal("/Admin/Clients", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-create")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientId = $"e2e-create-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Clients/Create");
            await page.FillAsync("input[name='Client.ClientId']", clientId);
            await page.FillAsync("input[name='Client.ClientName']", Generated.NewDisplayName());
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details"));
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Details_Shows_Edit_And_Delete()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Details?id={clientDbId}");
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-delete")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Edit_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Index?id={clientDbId}");
            Assert.Equal("/Admin/Clients/Edit/Index", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#save-submit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Edit_Save_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Edit/Index?id={clientDbId}");
            await page.CheckAsync("#CoordinateLifetimeWithUserSession");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details"));

            await page.GotoAsync($"/Admin/Clients/Edit/Index?id={clientDbId}");
            await Assertions.Expect(page.Locator("#CoordinateLifetimeWithUserSession")).ToBeCheckedAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientId = $"e2e-delete-{Guid.NewGuid():N}";
        var clientDbId = await fixture.SeedClientAsync(clientId);

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Clients");
            await page.ClickAsync($"#delete-{clientDbId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await Assertions.Expect(page.Locator($"#delete-{clientDbId}")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_ApiResources_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources");
            Assert.Equal("/Admin/ApiResources", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_ApiScopes_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes");
            Assert.Equal("/Admin/ApiScopes", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_IdentityResources_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityResources");
            Assert.Equal("/Admin/IdentityResources", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_IdentityProviders_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityProviders");
            Assert.Equal("/Admin/IdentityProviders", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlServiceProviders_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlServiceProviders");
            Assert.Equal("/Admin/SamlServiceProviders", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_PersistedGrants_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/PersistedGrants");
            Assert.Equal("/Admin/PersistedGrants", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_DeviceFlowCodes_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/DeviceFlowCodes");
            Assert.Equal("/Admin/DeviceFlowCodes", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_ServerSideSessions_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ServerSideSessions");
            Assert.Equal("/Admin/ServerSideSessions", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Keys_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Keys");
            Assert.Equal("/Admin/Keys", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_PushedAuthorizationRequests_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/PushedAuthorizationRequests");
            Assert.Equal("/Admin/PushedAuthorizationRequests", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlSigninStates_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlSigninStates");
            Assert.Equal("/Admin/SamlSigninStates", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlLogoutSessions_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlLogoutSessions");
            Assert.Equal("/Admin/SamlLogoutSessions", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlLogoutSessionRequestIndices_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlLogoutSessionRequestIndices");
            Assert.Equal("/Admin/SamlLogoutSessionRequestIndices", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Users_Index_Lists_Admin_User()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            var userId = await fixture.GetUserIdAsync(email);
            await page.GotoAsync("/Admin/Users");
            Assert.Equal("/Admin/Users", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator($"#details-{userId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Users_Details_Shows_Sub_Nav()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            var userId = await fixture.GetUserIdAsync(email);
            await page.GotoAsync("/Admin/Users");
            await page.ClickAsync($"#details-{userId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details"));
            await Assertions.Expect(page.Locator("#nav-claims")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#nav-roles")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#nav-logins")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#nav-passkeys")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Roles_Index_Lists_Admin_Role()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            var adminRoleId = await fixture.GetRoleIdAsync(AuthorizationNames.AdminRole);
            await page.GotoAsync("/Admin/Roles");
            Assert.Equal("/Admin/Roles", new Uri(page.Url).AbsolutePath);
            await Assertions.Expect(page.Locator("#page-heading")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator($"#details-{adminRoleId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Roles_Create_Delete_Round_Trip()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Roles/Create");
            await page.FillAsync("input[name='RoleName']", roleName);
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"));

            var roleId = await fixture.GetRoleIdAsync(roleName);
            await page.GotoAsync("/Admin/Roles");
            await Assertions.Expect(page.Locator($"#details-{roleId}")).ToBeVisibleAsync();

            await page.ClickAsync($"#delete-{roleId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("Delete"));
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator($"#details-{roleId}")).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task Admin_Roles_Details_Shows_Nav_Links()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            var adminRoleId = await fixture.GetRoleIdAsync(AuthorizationNames.AdminRole);
            await page.GotoAsync("/Admin/Roles");
            await page.ClickAsync($"#details-{adminRoleId}");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"));
            await Assertions.Expect(page.Locator("#nav-claims")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#nav-users")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-delete")).ToBeVisibleAsync();
        }
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
