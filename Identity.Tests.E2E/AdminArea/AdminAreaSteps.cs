namespace Identity.Tests.E2E.AdminArea;

using System.Reflection;
using Identity.Pages.Admin;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AdminAreaSteps(BrowserScenario scenario)
{
    private const string AdminCardSelector = "[id^='admin-card-']";

    private AdminSection? _followedSection;

    private IReadOnlyList<AdminSection> Sections =>
        scenario.Fixture.Factory.Services.GetRequiredService<IOptions<IReadOnlyList<AdminSection>>>().Value;

    [When("they open the admin area")]
    public async Task WhenTheyOpenTheAdminArea()
    {
        await scenario.Page.GotoAsync(AuthorizationNames.AdminFolder);
    }

    [When("they follow the card for the {string} section from the admin area")]
    public async Task WhenTheyFollowTheCardForTheSectionFromTheAdminArea(string title)
    {
        _followedSection = Sections.Single(section => string.Equals(section.Title, title, StringComparison.Ordinal));
        await scenario.Page.GotoAsync(AuthorizationNames.AdminFolder);
        await scenario.Page.ClickAsync($"#{_followedSection.CardId}");
    }

    [Then("the navigation offers the admin area")]
    public async Task ThenTheNavigationOffersTheAdminArea()
    {
        await Assertions.Expect(scenario.Page.Locator("#admin-nav")).ToBeVisibleAsync();
    }

    [Then("the navigation greets the member")]
    public async Task ThenTheNavigationGreetsTheMember()
    {
        await Assertions.Expect(scenario.Page.Locator("#manage-nav")).ToBeVisibleAsync();
    }

    [Then("the navigation offers no admin area")]
    public async Task ThenTheNavigationOffersNoAdminArea()
    {
        await Assertions.Expect(scenario.Page.Locator("#admin-nav")).Not.ToBeVisibleAsync();
    }

    [Then("there is a card for every admin section")]
    public async Task ThenThereIsACardForEveryAdminSection()
    {
        await Assertions.Expect(scenario.Page.Locator(AdminCardSelector))
            .ToHaveCountAsync(scenario.Fixture.Settings.IndependentlyPinnedAdminCardCount);
        Assert.Equal(scenario.Fixture.Settings.IndependentlyPinnedAdminCardCount, Sections.Count);
        foreach (var section in Sections)
        {
            await Assertions.Expect(scenario.Page.Locator($"#{section.CardId}")).ToBeVisibleAsync();
        }

        var outline = Assert.Single(
            typeof(AdminAreaFeature).GetMethods(),
            method => method.GetCustomAttribute<TheoryAttribute>() is not null);
        var listedTitles = outline.GetCustomAttributes<InlineDataAttribute>()
            .Select(row => Assert.IsType<string>(row.Data[0]))
            .ToList();

        Assert.Equal(scenario.Fixture.Settings.IndependentlyPinnedAdminCardCount, listedTitles.Count);
        Assert.Equal(
            Sections.Select(section => section.Title).Order(StringComparer.Ordinal),
            listedTitles.Order(StringComparer.Ordinal));
    }

    [Then("that section opens its own list")]
    public async Task ThenThatSectionOpensItsOwnList()
    {
        var section = _followedSection ?? throw new InvalidOperationException("No section card was followed in this scenario.");
        await Assertions.Expect(scenario.Page.Locator("#page-heading")).ToBeVisibleAsync();
        await Assertions.Expect(scenario.Page.Locator("#page-table")).ToBeVisibleAsync();
        Assert.Equal(section.Path, new Uri(scenario.Page.Url).AbsolutePath);
    }
}
