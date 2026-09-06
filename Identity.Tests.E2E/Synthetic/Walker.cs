namespace Identity.Tests.E2E.Synthetic;

using Microsoft.Playwright;

internal static class Walker
{
    private const int ThinkTimeMinMs = 1_500;
    private const int ThinkTimeMaxMs = 4_000;

    public static async Task<int> WalkAsync(
        IPage page,
        IReadOnlyList<WalkerAction> actions,
        uint seed,
        int steps)
    {
        var rng = new WalkerRng(seed);
        var executedSteps = 0;

        for (var stepIndex = 1; stepIndex <= steps; stepIndex += 1)
        {
            var availability = await Task.WhenAll(actions.Select(action => action.AvailableAsync(page)));
            var available = actions.Where((_, index) => availability[index]).ToList();
            if (available.Count == 0)
            {
                throw new InvalidOperationException(
                    $"seed={seed} step={stepIndex}: no action is available at {page.Url}; every walker needs at least one always-available action.");
            }

            var action = PickWeighted(rng, available);
            try
            {
                await action.RunAsync(page, rng);
            }
            catch (Exception cause)
            {
                throw new InvalidOperationException(
                    $"seed={seed} step={stepIndex} action={action.Name}: {cause.Message}", cause);
            }

            executedSteps += 1;
            await page.WaitForTimeoutAsync(ThinkTimeMinMs + rng.Int(ThinkTimeMaxMs - ThinkTimeMinMs));
        }

        return executedSteps;
    }

    private static WalkerAction PickWeighted(WalkerRng rng, IReadOnlyList<WalkerAction> actions)
    {
        var totalWeight = actions.Sum(action => action.Weight);
        var remaining = rng.Next() * totalWeight;
        foreach (var action in actions)
        {
            remaining -= action.Weight;
            if (remaining < 0)
            {
                return action;
            }
        }

        return actions[^1];
    }
}