namespace Identity.Tests.E2E.AdminClients;

using Identity.Tests.E2E.AdminLists;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AdminClientsSteps(BrowserScenario scenario, ListOwner owner)
{
    [Given("a client")]
    public async Task GivenAClient()
    {
        var clientDbId = await scenario.Fixture.SeedClientAsync($"e2e-client-{Guid.NewGuid():N}");
        owner.Set(ListOwnerKind.Client, clientDbId);
    }

    [When("they open the client list")]
    public async Task WhenTheyOpenTheClientList()
    {
        await scenario.Page.GotoAsync("/Admin/Clients");
    }

    [When("they register a new client")]
    public async Task WhenTheyRegisterANewClient()
    {
        await scenario.Page.GotoAsync("/Admin/Clients/Create");
        var clientId = $"e2e-create-{Guid.NewGuid():N}";
        await scenario.Page.FillAsync("input[name='Client.ClientId']", clientId);
        await scenario.Page.FillAsync("input[name='Client.ClientName']", Generated.NewDisplayName());
        await scenario.Page.ClickAsync("#create-submit");
    }

    [When("they open the client's details")]
    public async Task WhenTheyOpenTheClientsDetails()
    {
        await scenario.Page.GotoAsync($"/Admin/Clients/Details?id={owner.Id}");
    }

    [When("they open the client's settings for editing")]
    public async Task WhenTheyOpenTheClientsSettingsForEditing()
    {
        await scenario.Page.GotoAsync($"/Admin/Clients/Edit/Index?id={owner.Id}");
    }

    [When("they change a client setting and save")]
    public async Task WhenTheyChangeAClientSettingAndSave()
    {
        await scenario.Page.GotoAsync($"/Admin/Clients/Edit/Index?id={owner.Id}");
        await scenario.Page.CheckAsync("#CoordinateLifetimeWithUserSession");
        await scenario.Page.ClickAsync("#save-submit");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminClientsDetails());
    }

    [When("they delete the client from the client list")]
    public async Task WhenTheyDeleteTheClientFromTheClientList()
    {
        await scenario.Page.GotoAsync("/Admin/Clients");
        await scenario.Page.ClickAsync($"#delete-{owner.Id}");
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.DeletePage());
        await scenario.Page.ClickAsync("#delete-submit");
    }

    [Then("the client list is shown with a way to register a client")]
    public async Task ThenTheClientListIsShownWithAWayToRegisterAClient()
    {
        Assert.Equal("/Admin/Clients", new Uri(scenario.Page.Url).AbsolutePath);
        await Assertions.Expect(scenario.Page.Locator("#page-heading")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#btn-create")).ToBeVisibleAsync();
    }

    [Then("they see the new client's details")]
    public async Task ThenTheySeeTheNewClientsDetails()
    {
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(UrlPatterns.AdminClientsDetails());
        await Assertions.Expect(scenario.Page.Locator("#btn-edit")).ToBeVisibleAsync();
    }

    [Then("they can edit or delete the client")]
    public async Task ThenTheyCanEditOrDeleteTheClient()
    {
        await Assertions.Expect(scenario.Page.Locator("#btn-edit")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#btn-delete")).ToBeVisibleAsync();
    }

    [Then("the settings form is ready to save")]
    public async Task ThenTheSettingsFormIsReadyToSave()
    {
        Assert.Equal("/Admin/Clients/Edit/Index", new Uri(scenario.Page.Url).AbsolutePath);
        await Assertions.Expect(scenario.Page.Locator("#page-heading")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#save-submit")).ToBeVisibleAsync();
    }

    [Then("the changed setting is kept")]
    public async Task ThenTheChangedSettingIsKept()
    {
        await scenario.Page.GotoAsync($"/Admin/Clients/Edit/Index?id={owner.Id}");
        await Assertions.Expect(scenario.Page.Locator("#CoordinateLifetimeWithUserSession")).ToBeCheckedAsync();
    }

    [Then("the client is no longer listed")]
    public async Task ThenTheClientIsNoLongerListed()
    {
        await Assertions.Expect(scenario.Page).Not.ToHaveURLAsync(UrlPatterns.DeleteAnywhere());
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator($"#delete-{owner.Id}")).ToHaveCountAsync(0);
    }
}
