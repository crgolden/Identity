namespace Identity.Tests.E2E.Synthetic;

using System.Globalization;
using Microsoft.Playwright;

[Trait("Category", "Walker")]
public sealed class IdentityWalkerTests : IClassFixture<IdentityWalkerFixture>
{
    private const int DefaultStepBudget = 40;
    private const int MaxStepBudget = 500;
    private const int NonAdminSlot = 3;
    private const int AdminSlot = 1;
    private const string AdminCardSelector = "[id^='admin-card-']";

    private static readonly string[] ManageSectionIds =
    [
        "profile",
        "email",
        "change-password",
        "two-factor",
        "passkeys",
        "personal-data",
        "permissions",
    ];

    private readonly IdentityWalkerFixture _fixture;

    public IdentityWalkerTests(IdentityWalkerFixture fixture) => _fixture = fixture;

    private static IReadOnlyList<WalkerAction> MemberActions =>
    [
        NavigateAction("home", 3, "#nav-home"),
        NavigateAction("privacy", 1, "#nav-privacy"),
        NavigateAction("manage", 3, "#manage-nav"),
        new WalkerAction(
            "manage-section",
            4,
            page => AnyVisibleAsync(page, ManageSectionIds),
            async (page, rng) =>
            {
                var visible = await VisibleIdsAsync(page, ManageSectionIds);
                await page.ClickAsync($"#{rng.Pick(visible)}");
            }),
    ];

    private static IReadOnlyList<WalkerAction> AdminActions =>
    [
        .. MemberActions,
        NavigateAction("admin", 3, "#admin-nav"),
        new WalkerAction(
            "admin-section",
            4,
            async page => await page.Locator(AdminCardSelector).CountAsync() > 0,
            async (page, rng) =>
            {
                var cards = page.Locator(AdminCardSelector);
                await cards.Nth(rng.Int(await cards.CountAsync())).ClickAsync();
            }),
    ];

    [Fact]
    public async Task Walks_the_deployed_menus_as_a_member_then_as_an_admin()
    {
        Assert.SkipUnless(
            IdentityWalkerFixture.IsConfigured,
            "Synthetic walks target the deployed app only; set WalkerBaseUrl to run.");

        var seed = ResolveSeed();
        var steps = ResolveStepBudget();
        var memberSteps = steps / 2;
        var adminSteps = steps - memberSteps;

        var (context, page) = await _fixture.NewPageAsync("Walker");
        await using (context)
        {
            await SyntheticAccount.Resolve(NonAdminSlot).SignInAsync(page);
            var memberExecuted = await Walker.WalkAsync(page, MemberActions, seed, memberSteps);
            Assert.Equal(memberSteps, memberExecuted);

            await SyntheticAccount.SignOutAsync(page);

            await SyntheticAccount.Resolve(AdminSlot).SignInAsync(page);
            var adminExecuted = await Walker.WalkAsync(page, AdminActions, seed + 1, adminSteps);
            Assert.Equal(adminSteps, adminExecuted);
        }
    }

    private static WalkerAction NavigateAction(string name, int weight, string selector) =>
        new(
            name,
            weight,
            page => page.Locator(selector).IsVisibleAsync(),
            (page, _) => page.ClickAsync(selector));

    private static async Task<bool> AnyVisibleAsync(IPage page, IReadOnlyList<string> ids) =>
        (await VisibleIdsAsync(page, ids)).Count > 0;

    private static async Task<IReadOnlyList<string>> VisibleIdsAsync(IPage page, IReadOnlyList<string> ids)
    {
        var visibility = await Task.WhenAll(ids.Select(id => page.Locator($"#{id}").IsVisibleAsync()));
        return [.. ids.Where((_, index) => visibility[index])];
    }

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

    private static int ResolveStepBudget()
    {
        var raw = Environment.GetEnvironmentVariable("SYNTHETIC_STEPS");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultStepBudget;
        }

        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var steps)
            || steps < 2
            || steps > MaxStepBudget)
        {
            throw new InvalidOperationException(
                $"SYNTHETIC_STEPS must be an integer between 2 and {MaxStepBudget}; got {raw}.");
        }

        return steps;
    }
}