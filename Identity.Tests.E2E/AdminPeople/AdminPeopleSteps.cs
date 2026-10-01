namespace Identity.Tests.E2E.AdminPeople;

using System.Globalization;
using System.Text.RegularExpressions;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AdminPeopleSteps(BrowserScenario scenario, Member member)
{
    private string? _roleName;
    private string? _otherRoleName;
    private string? _claimType;
    private string? _changedValue;
    private string? _phoneNumber;
    private Guid? _roleIdBeforeRename;

    private string RoleName => _roleName ?? throw new InvalidOperationException("No role was created in this scenario.");

    private string OtherRoleName => _otherRoleName ?? throw new InvalidOperationException("No second role was created in this scenario.");

    private string ClaimType => _claimType ?? throw new InvalidOperationException("No claim was added in this scenario.");

    private string ChangedValue => _changedValue ?? throw new InvalidOperationException("No claim was changed in this scenario.");

    private string PhoneNumber => _phoneNumber ?? throw new InvalidOperationException("No phone number was entered in this scenario.");

    private Guid RoleIdBeforeRename => _roleIdBeforeRename ?? throw new InvalidOperationException("No role was renamed in this scenario.");

    [Given("a role")]
    public async Task GivenARole()
    {
        _roleName = $"e2e-role-{Guid.NewGuid():N}";
        await CreateRoleAsync(RoleName);
    }

    [Given("another role")]
    public async Task GivenAnotherRole()
    {
        _otherRoleName = $"e2e-role-other-{Guid.NewGuid():N}";
        await CreateRoleAsync(OtherRoleName);
    }

    [Given("they have opened their own account")]
    [When("they open their own account")]
    public async Task OpenOwnAccount()
    {
        var userId = await member.GetIdAsync();
        await scenario.Page.GotoAsync("/Admin/Users", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await scenario.Page.ClickAsync($"#details-{userId}");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersDetailsIndex());
    }

    [Given("the account holds a claim")]
    [When("they add a claim to the account")]
    public async Task AddClaimToAccount()
    {
        _claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        await AddClaimRowAsync(UrlPatterns.AdminUsersEditClaims(), UrlPatterns.AdminUsersDetailsClaims());
    }

    [Given("the account holds that role")]
    [When("they give the account that role")]
    public async Task GiveAccountTheRole()
    {
        await scenario.Page.ClickAsync("#nav-roles");
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersEditRoles());
        await scenario.Page.FillAsync($"#{Pages.Admin.Users.Edit.Roles.RowIdPrefix}{await AddRoleRowAsync()}", RoleName);
        await scenario.Page.ClickAsync("#save-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersDetailsRoles());
    }

    [Given("they have opened the admin role")]
    [When("they open the admin role")]
    public async Task OpenAdminRole()
    {
        var adminRoleId = await scenario.Fixture.GetRoleIdAsync(AuthorizationNames.AdminRole);
        await scenario.Page.GotoAsync("/Admin/Roles");
        await scenario.Page.ClickAsync($"#details-{adminRoleId}");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminRolesDetailsIndex());
    }

    [Given("the role holds a claim")]
    [When("they add a claim to the role")]
    public async Task AddClaimToRole()
    {
        _claimType = $"e2e-claimtype-{Guid.NewGuid():N}";
        await AddClaimRowAsync(UrlPatterns.AdminRolesEditClaims(), UrlPatterns.AdminRolesDetailsClaims());
    }

    [When("they open the user list")]
    public async Task WhenTheyOpenTheUserList()
    {
        await scenario.Page.GotoAsync("/Admin/Users");
    }

    [When("they open the account's {word}")]
    public async Task WhenTheyOpenTheAccountsPart(AccountPart part)
    {
        await scenario.Page.ClickAsync(NavId(part));
    }

    [When("they open the roles the account holds")]
    public async Task WhenTheyOpenTheRolesTheAccountHolds()
    {
        await scenario.Page.ClickAsync("#nav-roles");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersDetailsRoles());
    }

    [When("they change the account's phone number")]
    public async Task WhenTheyChangeTheAccountsPhoneNumber()
    {
        _phoneNumber = $"555{Random.Shared.Next(1000000, 9999999)}";
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersEditIndex());
        await scenario.Page.FillAsync("#AppUser_PhoneNumber", PhoneNumber);
        await scenario.Page.ClickAsync("#save-submit");
    }

    [When("they remove that claim from the account")]
    public async Task WhenTheyRemoveThatClaimFromTheAccount()
    {
        await RemoveClaimRowAsync(UrlPatterns.AdminUsersEditClaims(), UrlPatterns.AdminUsersDetailsClaims());
    }

    [When("they change that claim's value on the account")]
    public async Task WhenTheyChangeThatClaimsValueOnTheAccount()
    {
        await ChangeClaimRowAsync(UrlPatterns.AdminUsersEditClaims(), UrlPatterns.AdminUsersDetailsClaims());
    }

    [When("they take that role away from the account")]
    public async Task WhenTheyTakeThatRoleAwayFromTheAccount()
    {
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersEditRoles());
        await scenario.Page.ClickAsync($"#{Pages.Admin.Users.Edit.Roles.RemoveRowIdPrefix}{await RoleRowIndexAsync(RoleName)}");
        await scenario.Page.ClickAsync("#save-submit");
    }

    [When("they swap that role for the other role")]
    public async Task WhenTheySwapThatRoleForTheOtherRole()
    {
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersEditRoles());
        await scenario.Page.FillAsync($"#{Pages.Admin.Users.Edit.Roles.RowIdPrefix}{await RoleRowIndexAsync(RoleName)}", OtherRoleName);
        await scenario.Page.ClickAsync("#save-submit");
    }

    [When("they open the role list")]
    public async Task WhenTheyOpenTheRoleList()
    {
        await scenario.Page.GotoAsync("/Admin/Roles");
    }

    [When("they create a role")]
    public async Task WhenTheyCreateARole()
    {
        await GivenARole();
    }

    [When("they delete that role from the role list")]
    public async Task WhenTheyDeleteThatRoleFromTheRoleList()
    {
        var roleId = await scenario.Fixture.GetRoleIdAsync(RoleName);
        await scenario.Page.GotoAsync("/Admin/Roles");
        await Assertions.Expect(scenario.Page.Locator($"#details-{roleId}")).ToBeVisibleAsync();
        _roleIdBeforeRename = roleId;
        await scenario.Page.ClickAsync($"#delete-{roleId}");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.DeleteAnywhere());
        await scenario.Page.ClickAsync("#delete-submit");
    }

    [When("they open the role's claims")]
    public async Task WhenTheyOpenTheRolesClaims()
    {
        await scenario.Page.ClickAsync("#nav-claims");
    }

    [When("they open the role's users")]
    public async Task WhenTheyOpenTheRolesUsers()
    {
        await scenario.Page.ClickAsync("#nav-users");
    }

    [When("they rename that role")]
    public async Task WhenTheyRenameThatRole()
    {
        _roleIdBeforeRename = await scenario.Fixture.GetRoleIdAsync(RoleName);
        _otherRoleName = $"e2e-role-renamed-{Guid.NewGuid():N}";
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminRolesEditIndex());
        await scenario.Page.FillAsync("#AppRole_Name", OtherRoleName);
        await scenario.Page.ClickAsync("#save-submit");
    }

    [When("they remove that claim from the role")]
    public async Task WhenTheyRemoveThatClaimFromTheRole()
    {
        await RemoveClaimRowAsync(UrlPatterns.AdminRolesEditClaims(), UrlPatterns.AdminRolesDetailsClaims());
    }

    [When("they change that claim's value on the role")]
    public async Task WhenTheyChangeThatClaimsValueOnTheRole()
    {
        await ChangeClaimRowAsync(UrlPatterns.AdminRolesEditClaims(), UrlPatterns.AdminRolesDetailsClaims());
    }

    [Then("their own account is listed")]
    public async Task ThenTheirOwnAccountIsListed()
    {
        Assert.Equal("/Admin/Users", new Uri(scenario.Page.Url).AbsolutePath);
        await Assertions.Expect(scenario.Page.Locator("#page-heading")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator($"#details-{await member.GetIdAsync()}")).ToBeVisibleAsync();
    }

    [Then("they can reach the account's claims, roles, logins and passkeys")]
    public async Task ThenTheyCanReachTheAccountsParts()
    {
        await Assertions.Expect(scenario.Page.Locator("#nav-claims")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#nav-roles")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#nav-logins")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#nav-passkeys")).ToBeVisibleAsync();
    }

    [Then("the account's {word} are shown")]
    public async Task ThenTheAccountsPartIsShown(AccountPart part)
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(PartUrl(part));
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
    }

    [Then("there is one row for each of those roles")]
    public async Task ThenThereIsOneRowForEachOfThoseRoles()
    {
        var userId = await member.GetIdAsync();
        var roleCount = await scenario.Fixture.CountAsync<IdentityUserRole<Guid>>(r => r.UserId == userId);
        await Assertions.Expect(scenario.Page.Locator("[id^='user-role-']")).ToHaveCountAsync(roleCount);
    }

    [Then("the new phone number is kept")]
    public async Task ThenTheNewPhoneNumberIsKept()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersDetailsIndex());
        await Assertions.Expect(scenario.Page.Locator("#user-phone-number")).ToBeVisibleAsync();
        var email = member.Email;
        Assert.Equal(PhoneNumber, await scenario.Fixture.GetSingleAsync<IdentityUser<Guid>, string?>(u => u.Email == email, u => u.PhoneNumber));
    }

    [Then("the account holds the new claim")]
    public async Task ThenTheAccountHoldsTheNewClaim()
    {
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        var claimType = ClaimType;
        Assert.Equal(await member.GetIdAsync(), await scenario.Fixture.GetSingleAsync<IdentityUserClaim<Guid>, Guid>(c => c.ClaimType == claimType, c => c.UserId));
    }

    [Then("the account no longer holds the claim")]
    public async Task ThenTheAccountNoLongerHoldsTheClaim()
    {
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        var claimType = ClaimType;
        Assert.False(await scenario.Fixture.AnyAsync<IdentityUserClaim<Guid>>(c => c.ClaimType == claimType));
    }

    [Then("the account's claim has the new value")]
    public async Task ThenTheAccountsClaimHasTheNewValue()
    {
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        var claimType = ClaimType;
        Assert.Equal(ChangedValue, await scenario.Fixture.GetSingleAsync<IdentityUserClaim<Guid>, string?>(c => c.ClaimType == claimType, c => c.ClaimValue));
    }

    [Then("the account holds the role")]
    public async Task ThenTheAccountHoldsTheRole()
    {
        await Assertions.Expect(scenario.Page.Locator("#page-list")).ToBeVisibleAsync();
        Assert.True(await AccountHoldsAsync(RoleName));
    }

    [Then("the account no longer holds the role")]
    public async Task ThenTheAccountNoLongerHoldsTheRole()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersDetailsRoles());
        await Assertions.Expect(scenario.Page.Locator("#page-list")).ToBeVisibleAsync();
        Assert.False(await AccountHoldsAsync(RoleName));
    }

    [Then("the account holds only the other role")]
    public async Task ThenTheAccountHoldsOnlyTheOtherRole()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminUsersDetailsRoles());
        await Assertions.Expect(scenario.Page.Locator("#page-list")).ToBeVisibleAsync();
        Assert.True(await AccountHoldsAsync(OtherRoleName));
        Assert.False(await AccountHoldsAsync(RoleName));
    }

    [Then("the admin role is listed")]
    public async Task ThenTheAdminRoleIsListed()
    {
        Assert.Equal("/Admin/Roles", new Uri(scenario.Page.Url).AbsolutePath);
        await Assertions.Expect(scenario.Page.Locator("#page-heading")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        var adminRoleId = await scenario.Fixture.GetRoleIdAsync(AuthorizationNames.AdminRole);
        await Assertions.Expect(scenario.Page.Locator($"#details-{adminRoleId}")).ToBeVisibleAsync();
    }

    [Then("the role is no longer listed")]
    public async Task ThenTheRoleIsNoLongerListed()
    {
        await Assertions.Expect(scenario.Page).Not.ToHaveURLAsync(UrlPatterns.DeleteAnywhere());
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator($"#details-{RoleIdBeforeRename}")).ToHaveCountAsync(0);
    }

    [Then("they can reach the role's claims and users and can edit or delete it")]
    public async Task ThenTheyCanReachTheRolesClaimsAndUsers()
    {
        await Assertions.Expect(scenario.Page.Locator("#nav-claims")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#nav-users")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#btn-edit")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#btn-delete")).ToBeVisibleAsync();
    }

    [Then("the role's claims are shown")]
    public async Task ThenTheRolesClaimsAreShown()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminRolesDetailsClaims());
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
    }

    [Then("their own account is among the role's users")]
    public async Task ThenTheirOwnAccountIsAmongTheRolesUsers()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminRolesDetailsUsers());
        await Assertions.Expect(scenario.Page.Locator($"#user-row-{await member.GetIdAsync()}")).ToBeVisibleAsync();
    }

    [Then("the role keeps its identity under the new name")]
    public async Task ThenTheRoleKeepsItsIdentityUnderTheNewName()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminRolesDetailsIndex());
        await Assertions.Expect(scenario.Page.Locator("#role-name")).ToBeVisibleAsync();
        Assert.Equal(RoleIdBeforeRename, await scenario.Fixture.GetRoleIdAsync(OtherRoleName));
    }

    [Then("the role holds the new claim")]
    public async Task ThenTheRoleHoldsTheNewClaim()
    {
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        var claimType = ClaimType;
        Assert.Equal(
            await scenario.Fixture.GetRoleIdAsync(RoleName),
            await scenario.Fixture.GetSingleAsync<IdentityRoleClaim<Guid>, Guid>(c => c.ClaimType == claimType, c => c.RoleId));
    }

    [Then("the role no longer holds the claim")]
    public async Task ThenTheRoleNoLongerHoldsTheClaim()
    {
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        var claimType = ClaimType;
        Assert.False(await scenario.Fixture.AnyAsync<IdentityRoleClaim<Guid>>(c => c.ClaimType == claimType));
    }

    [Then("the role's claim has the new value")]
    public async Task ThenTheRolesClaimHasTheNewValue()
    {
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        var claimType = ClaimType;
        Assert.Equal(ChangedValue, await scenario.Fixture.GetSingleAsync<IdentityRoleClaim<Guid>, string?>(c => c.ClaimType == claimType, c => c.ClaimValue));
    }

    private static string NavId(AccountPart part) => part switch
    {
        AccountPart.Claims => "#nav-claims",
        AccountPart.Logins => "#nav-logins",
        AccountPart.Passkeys => "#nav-passkeys",
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    private static Regex PartUrl(AccountPart part) => part switch
    {
        AccountPart.Claims => UrlPatterns.AdminUsersDetailsClaims(),
        AccountPart.Logins => UrlPatterns.AdminUsersDetailsLogins(),
        AccountPart.Passkeys => UrlPatterns.AdminUsersDetailsPasskeys(),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    private async Task CreateRoleAsync(string roleName)
    {
        await scenario.Page.GotoAsync("/Admin/Roles/Create");
        await scenario.Page.FillAsync("#RoleName", roleName);
        await scenario.Page.ClickAsync("#create-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminRolesDetails());
    }

    private async Task AddClaimRowAsync(Regex editUrl, Regex detailsUrl)
    {
        await scenario.Page.ClickAsync("#nav-claims");
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(editUrl);
        await scenario.Page.ClickAsync("#btn-add-row");
        await scenario.Page.FillAsync("#claim-type-0", ClaimType);
        await scenario.Page.FillAsync("#claim-value-0", Generated.NewPropertyValue());
        await scenario.Page.ClickAsync("#save-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(detailsUrl);
    }

    private async Task RemoveClaimRowAsync(Regex editUrl, Regex detailsUrl)
    {
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(editUrl);
        await scenario.Page.ClickAsync("#claim-remove-0");
        await scenario.Page.ClickAsync("#save-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(detailsUrl);
    }

    private async Task ChangeClaimRowAsync(Regex editUrl, Regex detailsUrl)
    {
        _changedValue = $"e2e-updated-{Guid.NewGuid():N}";
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(editUrl);
        await scenario.Page.FillAsync("#claim-value-0", ChangedValue);
        await scenario.Page.ClickAsync("#save-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(detailsUrl);
    }

    private async Task<int> AddRoleRowAsync()
    {
        var addRowButton = scenario.Page.Locator("#btn-add-row");
        await Assertions.Expect(addRowButton).ToBeVisibleAsync();
        var rows = scenario.Page.Locator($"input[id^='{Pages.Admin.Users.Edit.Roles.RowIdPrefix}']");
        var addedRowIndex = await rows.CountAsync();
        await addRowButton.ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(addedRowIndex + 1);
        return addedRowIndex;
    }

    private async Task<int> RoleRowIndexAsync(string value)
    {
        var input = scenario.Page.Locator($"input[id^='{Pages.Admin.Users.Edit.Roles.RowIdPrefix}'][value='{value}']");
        await Assertions.Expect(input).ToHaveCountAsync(1);
        var id = await input.GetAttributeAsync("id");
        Assert.NotNull(id);
        return int.Parse(id[Pages.Admin.Users.Edit.Roles.RowIdPrefix.Length..], CultureInfo.InvariantCulture);
    }

    private async Task<bool> AccountHoldsAsync(string roleName)
    {
        var userId = await member.GetIdAsync();
        var roleId = await scenario.Fixture.GetRoleIdAsync(roleName);
        return await scenario.Fixture.AnyAsync<IdentityUserRole<Guid>>(r => r.UserId == userId && r.RoleId == roleId);
    }
}
