namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class RolesTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Details_Claims_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToAdminRoleDetailsAsync(page);

            await page.ClickAsync("#nav-claims");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Roles.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Details_Users_Shows_Admin_User()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            var userId = await fixture.GetUserIdAsync(email);
            await NavigateToAdminRoleDetailsAsync(page);

            await page.ClickAsync("#nav-users");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Roles.Details.Users.PageName));
            await Assertions.Expect(page.Locator($"#user-row-{userId}")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Index_Rename_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";
        var renamedTo = $"e2e-role-renamed-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            var roleId = await fixture.GetRoleIdAsync(roleName);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/(?!Claims|Users)"));
            await page.FillAsync("#AppRole_Name", renamedTo);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/(?!Claims|Users)"));
            await Assertions.Expect(page.Locator("#role-name")).ToBeVisibleAsync();
            Assert.Equal(roleId, await fixture.GetRoleIdAsync(renamedTo));
        }
    }

    [Fact]
    public async Task Edit_Claims_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            var roleId = await fixture.GetRoleIdAsync(roleName);
            await AddRoleClaimRowAsync(page, claimType);

            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            Assert.Equal(roleId, await fixture.GetSingleAsync<IdentityRoleClaim<Guid>, Guid>(c => c.ClaimType == claimType, c => c.RoleId));
        }
    }

    [Fact]
    public async Task Edit_Claims_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await AddRoleClaimRowAsync(page, claimType);
            Assert.True(await fixture.AnyAsync<IdentityRoleClaim<Guid>>(c => c.ClaimType == claimType));

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/Claims"));
            await page.ClickAsync("#claim-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Roles.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            Assert.False(await fixture.AnyAsync<IdentityRoleClaim<Guid>>(c => c.ClaimType == claimType));
        }
    }

    [Fact]
    public async Task Edit_Claims_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await AddRoleClaimRowAsync(page, claimType);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/Claims"));
            await page.FillAsync("#claim-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Roles.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            Assert.Equal(updatedValue, await fixture.GetSingleAsync<IdentityRoleClaim<Guid>, string?>(c => c.ClaimType == claimType, c => c.ClaimValue));
        }
    }

    private static async Task AddRoleClaimRowAsync(IPage page, string claimType)
    {
        await page.ClickAsync("#nav-claims");
        await page.ClickAsync("#btn-edit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/Claims"));
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claim-type-0", claimType);
        await page.FillAsync("#claim-value-0", Generated.NewPropertyValue());
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Roles.Edit.Claims.DetailsPageName));
    }

    private static async Task CreateRoleAsync(IPage page, string roleName)
    {
        await page.GotoAsync("/Admin/Roles/Create");
        await page.FillAsync("#RoleName", roleName);
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"));
    }

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(PageRoutes.Login);
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
    }

    private async Task NavigateToAdminRoleDetailsAsync(IPage page)
    {
        var adminRoleId = await fixture.GetRoleIdAsync(AuthorizationNames.AdminRole);
        await page.GotoAsync("/Admin/Roles");
        await page.ClickAsync($"#details-{adminRoleId}");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/(?!Claims|Users)"));
    }
}
