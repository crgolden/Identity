namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AdminTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Admin_Nav_Link_Visible_When_AdminRole()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
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

        var (context, page) = await fixture.NewPageAsync("Admin");
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

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin");

            string[] cardIds =
            [
                "#admin-card-clients", "#admin-card-apiresources", "#admin-card-apiscopes",
                "#admin-card-identityresources", "#admin-card-identityproviders", "#admin-card-samlserviceproviders",
                "#admin-card-persistedgrants", "#admin-card-deviceflowcodes", "#admin-card-serversidesessions",
                "#admin-card-keys", "#admin-card-pushedauthorizationrequests", "#admin-card-samlsigninstates",
                "#admin-card-samllogoutsessions", "#admin-card-samllogoutsessionrequestindices",
                "#admin-card-users", "#admin-card-roles"
            ];

            foreach (var id in cardIds)
            {
                await Assertions.Expect(page.Locator(id)).ToBeVisibleAsync();
            }
        }
    }

    [Fact]
    public async Task Admin_Unauthenticated_Redirects_To_Login()
    {
        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await page.GotoAsync("/Admin");
            Assert.Contains("/Account/Login", page.Url);
        }
    }

    [Fact]
    public async Task Admin_Clients_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Clients");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Clients");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-create")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Create_Redirects_To_Details()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientId = $"e2e-create-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Clients/Create");
            await page.FillAsync("input[name='Client.ClientId']", clientId);
            await page.FillAsync("input[name='Client.ClientName']", "E2E Created Client");
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Clients/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Details_Shows_Edit_And_Delete()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientDbId = await fixture.SeedClientAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync($"/Admin/Clients/Details?id={clientDbId}");
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-delete")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Clients_Delete_Removes_From_Index()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var clientId = $"e2e-delete-{Guid.NewGuid():N}";
        var clientDbId = await fixture.SeedClientAsync(clientId);

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Clients");
            await page.ClickAsync($"#delete-{clientDbId}");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Delete");
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator($"#delete-{clientDbId}")).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_ApiResources_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiResources");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("API Resources");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_ApiScopes_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ApiScopes");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("API Scopes");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_IdentityResources_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityResources");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Identity Resources");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_IdentityProviders_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/IdentityProviders");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Identity Providers");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlServiceProviders_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlServiceProviders");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("SAML Service Providers");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_PersistedGrants_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/PersistedGrants");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Persisted Grants");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_DeviceFlowCodes_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/DeviceFlowCodes");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Device Flow Codes");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_ServerSideSessions_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/ServerSideSessions");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Server-Side Sessions");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Keys_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Keys");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Keys");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_PushedAuthorizationRequests_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/PushedAuthorizationRequests");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Pushed Authorization Requests");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlSigninStates_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlSigninStates");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("SAML Sign-In States");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlLogoutSessions_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlLogoutSessions");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("SAML Logout Sessions");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_SamlLogoutSessionRequestIndices_Index_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/SamlLogoutSessionRequestIndices");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("SAML Logout Session Request Indices");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Users_Index_Lists_Admin_User()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Users");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Users");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(email).First).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Users_Details_Shows_Sub_Nav()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Users");
            var detailsLink = page.Locator("tr", new PageLocatorOptions { HasText = email }).Locator("[id^='details-']").First;
            await detailsLink.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Roles");
            await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Roles");
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("Admin").First).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Roles_Create_Delete_Round_Trip()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Roles/Create");
            await page.FillAsync("input[name='RoleName']", roleName);
            await page.ClickAsync("#create-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.GotoAsync("/Admin/Roles");
            await Assertions.Expect(page.GetByText(roleName).First).ToBeVisibleAsync();

            var deleteLink = page.Locator("tr", new PageLocatorOptions { HasText = roleName }).Locator("[id^='delete-']").First;
            await deleteLink.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#delete-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("Delete"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(roleName)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Admin_Roles_Details_Shows_Nav_Links()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin/Roles");
            var detailsLink = page.Locator("tr", new PageLocatorOptions { HasText = "Admin" }).Locator("[id^='details-']").First;
            await detailsLink.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("#nav-claims")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#nav-users")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-edit")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#btn-delete")).ToBeVisibleAsync();
        }
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
