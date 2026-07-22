namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

// Covers Admin-E2E-Guide.md's U3-U9 (U1/U2 Index-lists/sub-nav-visibility already exist in
// AdminTests.cs). Each test creates its own unique admin user via fixture.CreateAdminUserAsync() --
// same convention as AdminTests.cs -- so Edit/Claims and Edit/Roles mutations never touch shared state.
// U10/U11 (Edit/Logins, Edit/Passkeys) are out of scope per the approved plan.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class UsersTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Details_Claims_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-claims");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Details_Roles_Shows_Admin_Role()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-roles");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("li.list-group-item", new PageLocatorOptions { HasText = "Admin" })).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Details_Logins_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-logins");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Logins"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Details_Passkeys_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-passkeys");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Passkeys"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Index_PhoneNumber_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var phoneNumber = $"555{Random.Shared.Next(1000000, 9999999)}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/(?!Claims|Roles|Logins|Passkeys)"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.FillAsync("#AppUser_PhoneNumber", phoneNumber);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/(?!Claims|Roles|Logins|Passkeys)"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(phoneNumber)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Claims_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-claims");
            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claim-type-0", claimType);
            await page.FillAsync("#claim-value-0", "e2e-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Claims_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserClaimRowAsync(page, claimType);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#claim-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Claims_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        var updatedValue = $"e2e-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserClaimRowAsync(page, claimType);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.FillAsync("#claim-value-0", updatedValue);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(updatedValue)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Roles_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-user-role-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await NavigateToOwnDetailsAsync(page, email);

            await page.ClickAsync("#nav-roles");
            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#role-1", roleName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("li.list-group-item", new PageLocatorOptions { HasText = roleName })).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Roles_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-user-role-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserRoleRowAsync(page, roleName);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#role-remove-1");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("li.list-group-item", new PageLocatorOptions { HasText = roleName })).Not.ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Roles_Update_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-user-role-{Guid.NewGuid():N}";
        var updatedRoleName = $"e2e-user-role-updated-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);
            await CreateRoleAsync(page, updatedRoleName);
            await NavigateToOwnDetailsAsync(page, email);
            await AddUserRoleRowAsync(page, roleName);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.FillAsync("#role-1", updatedRoleName);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("li.list-group-item", new PageLocatorOptions { HasText = updatedRoleName })).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("li.list-group-item", new PageLocatorOptions { HasText = roleName })).Not.ToBeVisibleAsync();
        }
    }

    private static async Task CreateRoleAsync(IPage page, string roleName)
    {
        await page.GotoAsync("/Admin/Roles/Create");
        await page.FillAsync("#RoleName", roleName);
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddUserClaimRowAsync(IPage page, string claimType)
    {
        await page.ClickAsync("#nav-claims");
        await page.ClickAsync("#btn-edit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#claim-type-0", claimType);
        await page.FillAsync("#claim-value-0", "e2e-value");
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task AddUserRoleRowAsync(IPage page, string roleName)
    {
        await page.ClickAsync("#nav-roles");
        await page.ClickAsync("#btn-edit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Edit/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
        await page.ClickAsync("#btn-add-row");
        await page.FillAsync("#role-1", roleName);
        await page.ClickAsync("#save-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/Roles"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task NavigateToOwnDetailsAsync(IPage page, string email)
    {
        await page.GotoAsync("/Admin/Users");
        var detailsLink = page.Locator("tr", new PageLocatorOptions { HasText = email }).Locator("[id^='details-']").First;
        await detailsLink.ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Users/Details/(?!Claims|Roles|Logins|Passkeys)"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
