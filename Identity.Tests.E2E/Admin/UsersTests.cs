namespace Identity.Tests.E2E.Admin;

using System.Globalization;
using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class UsersTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Details_Claims_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-claims");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Details_Roles_Lists_A_Row_Per_Role()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-roles");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Roles.DetailsPageName));
            var userId = await fixture.GetUserIdAsync(email);
            var roleCount = await fixture.CountAsync<IdentityUserRole<Guid>>(r => r.UserId == userId);
            await Assertions.Expect(page.Locator("[id^='user-role-']")).ToHaveCountAsync(roleCount);
        }
    }

    [Fact]
    public async Task Details_Logins_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-logins");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Details.Logins.PageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Details_Passkeys_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-passkeys");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Details.Passkeys.PageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Index_PhoneNumber_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var phoneNumber = $"555{Random.Shared.Next(1000000, 9999999)}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/(?!Claims|Roles|Logins|Passkeys)"));
            await page.FillAsync("#AppUser_PhoneNumber", phoneNumber);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/(?!Claims|Roles|Logins|Passkeys)"));
            await Assertions.Expect(page.Locator("#user-phone-number")).ToBeVisibleAsync();
            Assert.Equal(phoneNumber, await fixture.GetSingleAsync<IdentityUser<Guid>, string?>(u => u.Email == email, u => u.PhoneNumber));
        }
    }

    [Fact]
    public async Task Edit_Claims_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-claims");
            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"));
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claim-type-0", claimType);
            await page.FillAsync("#claim-value-0", Generated.NewPropertyValue());
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            var userId = await fixture.GetUserIdAsync(email);
            Assert.Equal(userId, await fixture.GetSingleAsync<IdentityUserClaim<Guid>, Guid>(c => c.ClaimType == claimType, c => c.UserId));
        }
    }

    [Fact]
    public async Task Edit_Claims_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserClaimRowAsync(page, claimType);
            Assert.True(await fixture.AnyAsync<IdentityUserClaim<Guid>>(c => c.ClaimType == claimType));

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"));
            await page.ClickAsync("#claim-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            Assert.False(await fixture.AnyAsync<IdentityUserClaim<Guid>>(c => c.ClaimType == claimType));
        }
    }

    [Fact]
    public async Task Edit_Claims_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserClaimRowAsync(page, claimType);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"));
            await page.FillAsync("#claim-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Claims.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-table")).ToBeVisibleAsync();
            Assert.Equal(updatedValue, await fixture.GetSingleAsync<IdentityUserClaim<Guid>, string?>(c => c.ClaimType == claimType, c => c.ClaimValue));
        }
    }

    [Fact]
    public async Task Edit_Roles_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-user-role-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-roles");
            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"));
            await page.FillAsync($"#{Pages.Admin.Users.Edit.Roles.RowIdPrefix}{await AddRoleRowAsync(page)}", roleName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Roles.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-list")).ToBeVisibleAsync();
            var userId = await fixture.GetUserIdAsync(email);
            var roleId = await fixture.GetRoleIdAsync(roleName);
            Assert.True(await fixture.AnyAsync<IdentityUserRole<Guid>>(r => r.UserId == userId && r.RoleId == roleId));
        }
    }

    [Fact]
    public async Task Edit_Roles_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-user-role-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserRoleRowAsync(page, roleName);
            var userId = await fixture.GetUserIdAsync(email);
            var roleId = await fixture.GetRoleIdAsync(roleName);
            Assert.True(await fixture.AnyAsync<IdentityUserRole<Guid>>(r => r.UserId == userId && r.RoleId == roleId));

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"));
            await page.ClickAsync($"#{Pages.Admin.Users.Edit.Roles.RemoveRowIdPrefix}{await RoleRowIndexAsync(page, roleName)}");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Roles.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-list")).ToBeVisibleAsync();
            Assert.False(await fixture.AnyAsync<IdentityUserRole<Guid>>(r => r.UserId == userId && r.RoleId == roleId));
        }
    }

    [Fact]
    public async Task Edit_Roles_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-user-role-{Guid.NewGuid():N}";
        var updatedRoleName = $"e2e-user-role-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Admin);
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await CreateRoleAsync(page, updatedRoleName);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserRoleRowAsync(page, roleName);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"));
            await page.FillAsync($"#{Pages.Admin.Users.Edit.Roles.RowIdPrefix}{await RoleRowIndexAsync(page, roleName)}", updatedRoleName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Roles.DetailsPageName));
            await Assertions.Expect(page.Locator("#page-list")).ToBeVisibleAsync();
            var userId = await fixture.GetUserIdAsync(email);
            var roleId = await fixture.GetRoleIdAsync(roleName);
            var updatedRoleId = await fixture.GetRoleIdAsync(updatedRoleName);
            Assert.True(await fixture.AnyAsync<IdentityUserRole<Guid>>(r => r.UserId == userId && r.RoleId == updatedRoleId));
            Assert.False(await fixture.AnyAsync<IdentityUserRole<Guid>>(r => r.UserId == userId && r.RoleId == roleId));
        }
    }

    private static async Task CreateRoleAsync(IPage page, string roleName)
    {
        await page.GotoAsync("/Admin/Roles/Create");
        await page.FillAsync("#RoleName", roleName);
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"));
    }

    private static async Task AddUserClaimRowAsync(IPage page, string claimType)
    {
        await page.ClickAsync("#nav-claims");
        await page.ClickAsync("#btn-edit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"));
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claim-type-0", claimType);
        await page.FillAsync("#claim-value-0", Generated.NewPropertyValue());
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Claims.DetailsPageName));
    }

    private static async Task AddUserRoleRowAsync(IPage page, string roleName)
    {
        await page.ClickAsync("#nav-roles");
        await page.ClickAsync("#btn-edit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"));
        await page.FillAsync($"#{Pages.Admin.Users.Edit.Roles.RowIdPrefix}{await AddRoleRowAsync(page)}", roleName);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(Pages.Admin.Users.Edit.Roles.DetailsPageName));
    }

    private static async Task<int> AddRoleRowAsync(IPage page)
    {
        var addRowButton = page.Locator("#btn-add-row");
        await Assertions.Expect(addRowButton).ToBeVisibleAsync();
        var rows = page.Locator($"input[id^='{Pages.Admin.Users.Edit.Roles.RowIdPrefix}']");
        var addedRowIndex = await rows.CountAsync();
        await addRowButton.ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(addedRowIndex + 1);
        return addedRowIndex;
    }

    private static async Task<int> RoleRowIndexAsync(IPage page, string value)
    {
        var input = page.Locator($"input[id^='{Pages.Admin.Users.Edit.Roles.RowIdPrefix}'][value='{value}']");
        await Assertions.Expect(input).ToHaveCountAsync(1);
        var id = await input.GetAttributeAsync("id");
        Assert.NotNull(id);
        return int.Parse(id[Pages.Admin.Users.Edit.Roles.RowIdPrefix.Length..], CultureInfo.InvariantCulture);
    }

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(PageRoutes.Login);
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
        await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));
    }

    private async Task NavigateToOwnDetailsAsync(IPage page, string email)
    {
        var userId = await fixture.GetUserIdAsync(email);
        await page.GotoAsync("/Admin/Users", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.ClickAsync($"#details-{userId}");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/(?!Claims|Roles|Logins|Passkeys)"));
    }
}
