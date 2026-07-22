namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

// Covers Admin-E2E-Guide.md's R3-R6 (R1/R2 Index-lists/Create-round-trip and Details nav-links already
// exist in AdminTests.cs). Details/Claims and Details/Users use the shared "Admin" role directly (safe --
// read-only). Edit/Index rename and Edit/Claims mutate a fresh, uniquely-named role created per test, so
// the shared "Admin" role used by fixture.CreateAdminUserAsync() across the whole suite is never touched.
[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class RolesTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Details_Claims_Loads()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToAdminRoleDetailsAsync(page);

            await page.ClickAsync("#nav-claims");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Details_Users_Shows_Admin_User()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await NavigateToAdminRoleDetailsAsync(page);

            await page.ClickAsync("#nav-users");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/Users"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(email).First).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Index_Rename_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";
        var renamedTo = $"e2e-role-renamed-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/(?!Claims|Users)"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.FillAsync("#AppRole_Name", renamedTo);
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/(?!Claims|Users)"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(renamedTo)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Claims_Add_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);

            await page.ClickAsync("#nav-claims");
            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claim-type-0", claimType);
            await page.FillAsync("#claim-value-0", "e2e-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task Edit_Claims_Remove_Persists()
    {
        var (email, password) = await fixture.CreateAdminUserAsync();
        var roleName = $"e2e-role-{Guid.NewGuid():N}";
        var claimType = $"e2e-claimtype-{Guid.NewGuid():N}";

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await CreateRoleAsync(page, roleName);

            await page.ClickAsync("#nav-claims");
            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#btn-add-row");
            await page.FillAsync("#claim-type-0", claimType);
            await page.FillAsync("#claim-value-0", "e2e-value");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });

            await page.ClickAsync("#btn-edit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Edit/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await page.ClickAsync("#claim-remove-0");
            await page.ClickAsync("#save-submit");
            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/Claims"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
            await Assertions.Expect(page.GetByText(claimType)).Not.ToBeVisibleAsync();
        }
    }

    private static async Task CreateRoleAsync(IPage page, string roleName)
    {
        await page.GotoAsync("/Admin/Roles/Create");
        await page.FillAsync("#RoleName", roleName);
        await page.ClickAsync("#create-submit");
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
    }

    private static async Task NavigateToAdminRoleDetailsAsync(IPage page)
    {
        await page.GotoAsync("/Admin/Roles");
        var detailsLink = page.Locator("tr", new PageLocatorOptions { HasText = "Admin" }).Locator("[id^='details-']").First;
        await detailsLink.ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/Admin/Roles/Details/(?!Claims|Users)"), new PageAssertionsToHaveURLOptions { Timeout = 60_000 });
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
