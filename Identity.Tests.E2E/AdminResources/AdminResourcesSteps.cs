namespace Identity.Tests.E2E.AdminResources;

using System.Text.RegularExpressions;
using Identity.Tests.E2E.AdminLists;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AdminResourcesSteps(BrowserScenario scenario, ListOwner owner)
{
    [Given("an existing {word}")]
    public async Task GivenAnExistingRecord(ListOwnerKind kind)
    {
        var name = $"e2e-{Section(kind)}-{Guid.NewGuid():N}";
        var id = kind switch
        {
            ListOwnerKind.ApiResource => await scenario.Fixture.SeedApiResourceAsync(name),
            ListOwnerKind.ApiScope => await scenario.Fixture.SeedApiScopeAsync(name),
            ListOwnerKind.IdentityResource => await scenario.Fixture.SeedIdentityResourceAsync(name),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        owner.Set(kind, id);
    }

    [When("they open the {word} section")]
    public async Task WhenTheyOpenTheList(ListOwnerKind kind)
    {
        await scenario.Page.GotoAsync($"{AuthorizationNames.AdminFolder}/{Section(kind)}");
    }

    [When("they create a new {word}")]
    public async Task WhenTheyCreateANewRecord(ListOwnerKind kind)
    {
        var (nameFieldId, displayNameFieldId) = kind == ListOwnerKind.ApiScope
            ? ("#Scope_Name", "#Scope_DisplayName")
            : ("#Resource_Name", "#Resource_DisplayName");
        var name = $"e2e-create-{Guid.NewGuid():N}";
        await scenario.Page.GotoAsync($"{AuthorizationNames.AdminFolder}/{Section(kind)}/Create");
        await scenario.Page.FillAsync(nameFieldId, name);
        await scenario.Page.FillAsync(displayNameFieldId, Generated.NewDisplayName());
        await scenario.Page.ClickAsync("#create-submit");
    }

    [When("they delete the {word} from its section")]
    public async Task WhenTheyDeleteTheRecordFromItsList(ListOwnerKind kind)
    {
        await scenario.Page.GotoAsync($"{AuthorizationNames.AdminFolder}/{Section(kind)}");
        await scenario.Page.ClickAsync($"#delete-{owner.Id}");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.DeletePage());
        await scenario.Page.ClickAsync("#delete-submit");
    }

    [Then("the {word} appears in its section")]
    public async Task ThenTheRecordIsListed(ListOwnerKind kind)
    {
        Assert.Equal($"{AuthorizationNames.AdminFolder}/{Section(kind)}", new Uri(scenario.Page.Url).AbsolutePath);
        await Assertions.Expect(scenario.Page.Locator($"#details-{owner.Id}")).ToBeVisibleAsync();
    }

    [Then("they see the details of the new {word}")]
    public async Task ThenTheySeeTheDetailsOfTheNewRecord(ListOwnerKind kind)
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(DetailsUrl(kind));
        await Assertions.Expect(scenario.Page.Locator("#btn-edit")).ToBeVisibleAsync();
    }

    [Then("the {word} no longer appears in its section")]
    public async Task ThenTheRecordIsNoLongerListed(ListOwnerKind kind)
    {
        await Assertions.Expect(scenario.Page).Not.ToHaveURLAsync(UrlPatterns.DeleteAnywhere());
        Assert.Equal($"{AuthorizationNames.AdminFolder}/{Section(kind)}", new Uri(scenario.Page.Url).AbsolutePath);
        await Assertions.Expect(scenario.Page.Locator($"#delete-{owner.Id}")).Not.ToBeVisibleAsync();
    }

    private static string Section(ListOwnerKind kind) => kind switch
    {
        ListOwnerKind.ApiResource => nameof(Pages.Admin.ApiResources),
        ListOwnerKind.ApiScope => nameof(Pages.Admin.ApiScopes),
        ListOwnerKind.IdentityResource => nameof(Pages.Admin.IdentityResources),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static Regex DetailsUrl(ListOwnerKind kind) => kind switch
    {
        ListOwnerKind.ApiResource => UrlPatterns.AdminApiResourcesDetails(),
        ListOwnerKind.ApiScope => UrlPatterns.AdminApiScopesDetails(),
        ListOwnerKind.IdentityResource => UrlPatterns.AdminIdentityResourcesDetails(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
