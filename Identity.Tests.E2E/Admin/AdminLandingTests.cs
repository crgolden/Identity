namespace Identity.Tests.E2E.Admin;

using System.Text.RegularExpressions;
using Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AdminLandingTests(PlaywrightFixture fixture)
{
    public static TheoryData<string, string> Cards => new()
    {
        { "admin-card-clients", "Clients" },
        { "admin-card-apiresources", "API Resources" },
        { "admin-card-apiscopes", "API Scopes" },
        { "admin-card-identityresources", "Identity Resources" },
        { "admin-card-identityproviders", "Identity Providers" },
        { "admin-card-samlserviceproviders", "SAML Service Providers" },
        { "admin-card-persistedgrants", "Persisted Grants" },
        { "admin-card-deviceflowcodes", "Device Flow Codes" },
        { "admin-card-serversidesessions", "Server-Side Sessions" },
        { "admin-card-keys", "Keys" },
        { "admin-card-pushedauthorizationrequests", "Pushed Authorization Requests" },
        { "admin-card-samlsigninstates", "SAML Sign-In States" },
        { "admin-card-samllogoutsessions", "SAML Logout Sessions" },
        { "admin-card-samllogoutsessionrequestindices", "SAML Logout Session Request Indices" },
        { "admin-card-users", "Users" },
        { "admin-card-roles", "Roles" },
    };

    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Card_Manage_Link_Navigates_To_Correct_Index(string cardId, string expectedHeading)
    {
        var (email, password) = await fixture.CreateAdminUserAsync();

        var (context, page) = await fixture.NewPageAsync("Admin");
        await using (context)
        {
            await LoginAsync(page, email, password);
            await page.GotoAsync("/Admin");
            await page.ClickAsync($"#{cardId}");
            await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(expectedHeading, new LocatorAssertionsToHaveTextOptions { Timeout = 60_000 });
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