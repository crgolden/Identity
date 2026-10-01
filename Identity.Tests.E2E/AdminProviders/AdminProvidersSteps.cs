namespace Identity.Tests.E2E.AdminProviders;

using System.Text.RegularExpressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AdminProvidersSteps(BrowserScenario scenario)
{
    private string? _key;
    private string? _newDisplayName;
    private int? _deletedProviderId;

    private string Key => _key ?? throw new InvalidOperationException("No provider was registered in this scenario.");

    private int DeletedProviderId => _deletedProviderId ?? throw new InvalidOperationException("No provider was deleted in this scenario.");

    private string NewDisplayName => _newDisplayName ?? throw new InvalidOperationException("No provider was renamed in this scenario.");

    [When("they add a new {word}")]
    [Given("a registered {word}")]
    public async Task RegisterAProvider(ProviderKind kind)
    {
        _key = $"e2e-provider-{Guid.NewGuid():N}";
        if (kind == ProviderKind.IdentityProvider)
        {
            await scenario.Page.GotoAsync("/Admin/IdentityProviders/Create");
            await scenario.Page.FillAsync("#IdentityProvider_Scheme", Key);
            await scenario.Page.FillAsync("#IdentityProvider_DisplayName", Generated.NewDisplayName());
            await scenario.Page.FillAsync("#IdentityProvider_Type", OidcStandardConstants.OidcProtocol);
        }
        else
        {
            await scenario.Page.GotoAsync("/Admin/SamlServiceProviders/Create");
            await scenario.Page.FillAsync("#SamlServiceProvider_EntityId", Key);
            await scenario.Page.FillAsync("#SamlServiceProvider_DisplayName", Generated.NewDisplayName());
        }

        await scenario.Page.ClickAsync("#create-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(DetailsUrl(kind));
    }

    [When("they rename the {word}")]
    public async Task WhenTheyRenameTheProvider(ProviderKind kind)
    {
        _newDisplayName = $"e2e-updated-{Guid.NewGuid():N}";
        await scenario.Page.ClickAsync("#btn-edit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(
            kind == ProviderKind.IdentityProvider ? UrlPatterns.AdminIdentityProvidersEdit() : UrlPatterns.AdminSamlServiceProvidersEdit());
        await scenario.Page.FillAsync(
            kind == ProviderKind.IdentityProvider ? "#IdentityProvider_DisplayName" : "#SamlServiceProvider_DisplayName",
            NewDisplayName);
        await scenario.Page.ClickAsync("#save-submit");
    }

    [When("they delete the {word} from its details")]
    public async Task WhenTheyDeleteTheProviderFromItsDetails(ProviderKind kind)
    {
        _deletedProviderId = await ProviderIdAsync(kind);
        await OpenListAsync(kind);
        await Assertions.Expect(scenario.Page.Locator($"#details-{DeletedProviderId}")).ToBeVisibleAsync();
        await scenario.Page.ClickAsync($"#details-{DeletedProviderId}");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(DetailsUrl(kind));
        await scenario.Page.ClickAsync("#btn-delete");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.DeletePage());
        await scenario.Page.ClickAsync("#delete-submit");
    }

    [Then("they land on the new {word}")]
    public async Task ThenTheyLandOnTheNewProvider(ProviderKind kind)
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(DetailsUrl(kind));
        await Assertions.Expect(scenario.Page.Locator("#btn-edit")).ToBeVisibleAsync();
    }

    [Then("the {word} shows its new name")]
    public async Task ThenTheProviderShowsItsNewName(ProviderKind kind)
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(DetailsUrl(kind));
        await Assertions.Expect(scenario.Page.Locator(kind == ProviderKind.IdentityProvider ? "#idp-display-name" : "#sp-display-name")).ToBeVisibleAsync();
        var persistedDisplayName = kind == ProviderKind.IdentityProvider
            ? await scenario.Fixture.GetSingleAsync<IdentityProvider, string>(p => p.Scheme == Key, p => p.DisplayName)
            : await scenario.Fixture.GetSingleAsync<SamlServiceProvider, string>(p => p.EntityId == Key, p => p.DisplayName);
        Assert.Equal(NewDisplayName, persistedDisplayName);
    }

    [Then("the {word} is no longer registered")]
    public async Task ThenTheProviderIsNoLongerRegistered(ProviderKind kind)
    {
        await Assertions.Expect(scenario.Page).Not.ToHaveURLAsync(UrlPatterns.DeleteAnywhere());
        await OpenListAsync(kind);
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator($"#details-{DeletedProviderId}")).ToHaveCountAsync(0);
    }

    private static Regex DetailsUrl(ProviderKind kind) =>
        kind == ProviderKind.IdentityProvider ? UrlPatterns.AdminIdentityProvidersDetails() : UrlPatterns.AdminSamlServiceProvidersDetails();

    private Task<IResponse?> OpenListAsync(ProviderKind kind) =>
        kind == ProviderKind.IdentityProvider
            ? scenario.Page.GotoAsync($"{AuthorizationNames.AdminFolder}/{nameof(Pages.Admin.IdentityProviders)}")
            : scenario.Page.GotoAsync($"{AuthorizationNames.AdminFolder}/{nameof(Pages.Admin.SamlServiceProviders)}");

    private Task<int> ProviderIdAsync(ProviderKind kind) =>
        kind == ProviderKind.IdentityProvider
            ? scenario.Fixture.GetSingleAsync<IdentityProvider, int>(p => p.Scheme == Key, p => p.Id)
            : scenario.Fixture.GetSingleAsync<SamlServiceProvider, int>(p => p.EntityId == Key, p => p.Id);
}
