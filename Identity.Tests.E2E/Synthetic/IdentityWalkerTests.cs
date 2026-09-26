namespace Identity.Tests.E2E.Synthetic;

using System.Globalization;
using Identity.Pages.Account.Manage;
using Identity.Pages.Admin;
using Identity.Tests.E2E.Infrastructure;

[Trait("Category", "Walker")]
public sealed class IdentityWalkerTests : IClassFixture<IdentityWalkerFixture>
{
    private const int PersonaCount = 2;
    private const string AdminCardSelector = $"[id^='{AdminSection.CardIdPrefix}']";
    private const string ManageSectionLinkSelector = $"[id^='{ManageSection.LinkIdPrefix}']";

    private readonly IdentityWalkerFixture _fixture;

    public IdentityWalkerTests(IdentityWalkerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Walks_the_deployed_menus_as_a_member_then_as_an_admin()
    {
        Assert.SkipUnless(
            IdentityWalkerFixture.IsConfigured,
            "Synthetic walks target the deployed app only; set WalkerBaseUrl to run.");

        var settings = _fixture.Settings;
        var seed = ResolveSeed();
        var steps = ResolveStepBudget(settings);
        var memberSteps = steps / PersonaCount;
        var adminSteps = steps - memberSteps;

        var (context, page) = await _fixture.NewPageAsync(PlaywrightSuite.Walker);
        await using (context)
        {
            await SyntheticAccount.Resolve(settings.MemberSlot).SignInAsync(page);
            var memberExecuted = await Walker.WalkAsync(page, MemberActions(settings), seed, memberSteps);
            Assert.Equal(memberSteps, memberExecuted);

            await SyntheticAccount.SignOutAsync(page);

            await SyntheticAccount.Resolve(settings.AdminSlot).SignInAsync(page);
            var adminExecuted = await Walker.WalkAsync(page, AdminActions(settings), seed + 1, adminSteps);
            Assert.Equal(adminSteps, adminExecuted);
        }
    }

    private static IReadOnlyList<WalkerAction> MemberActions(WalkerSettings settings) =>
    [
        NavigateAction(settings.CommonActionWeight, "#nav-home"),
        NavigateAction(settings.RareActionWeight, "#nav-privacy"),
        NavigateAction(settings.CommonActionWeight, "#manage-nav"),
        SectionAction(nameof(ManageSection), settings.SectionActionWeight, ManageSectionLinkSelector),
    ];

    private static IReadOnlyList<WalkerAction> AdminActions(WalkerSettings settings) =>
    [
        .. MemberActions(settings),
        NavigateAction(settings.CommonActionWeight, "#admin-nav"),
        SectionAction(nameof(AdminSection), settings.SectionActionWeight, AdminCardSelector),
    ];

    private static WalkerAction SectionAction(string name, int weight, string selector) =>
        new(
            name,
            weight,
            async page => await page.Locator(selector).CountAsync() > 0,
            async (page, rng) =>
            {
                var links = page.Locator(selector);
                await links.First.WaitForAsync();
                await links.Nth(rng.Int(await links.CountAsync())).ClickAsync();
            });

    private static WalkerAction NavigateAction(int weight, string selector) =>
        new(
            selector,
            weight,
            page => page.Locator(selector).IsVisibleAsync(),
            (page, _) => page.ClickAsync(selector));

    private static uint ResolveSeed()
    {
        var raw = Environment.GetEnvironmentVariable("SYNTHETIC_SEED");
        if (!uint.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
        {
            throw new InvalidOperationException(
                "SYNTHETIC_SEED must be set to a decimal uint32 so every walk is replayable.");
        }

        return seed;
    }

    private static int ResolveStepBudget(WalkerSettings settings) =>
        EnvironmentSetting.Count("SYNTHETIC_STEPS", settings.DefaultStepBudget, PersonaCount, settings.MaxStepBudget);
}
