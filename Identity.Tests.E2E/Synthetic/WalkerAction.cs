namespace Identity.Tests.E2E.Synthetic;

using Microsoft.Playwright;

internal sealed record WalkerAction(
    string Name,
    int Weight,
    Func<IPage, Task<bool>> AvailableAsync,
    Func<IPage, WalkerRng, Task> RunAsync);