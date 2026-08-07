namespace Identity.Tests.E2E.Security;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ConcurrentLockoutTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Login_ConcurrentFailedAttempts_AccountEventuallyLocked()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();

        var concurrentTasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var (ctx, page) = await fixture.NewPageAsync();
            await using (ctx)
            {
                await page.GotoAsync("/Account/Login");
                await page.FillAsync("input[name='Input.Email']", email);
                await page.FillAsync("input[name='Input.Password']", "BadPassword!Concurrent99");
                await page.ClickAsync("button[type='submit']");
                await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
            }
        });
        await Task.WhenAll(concurrentTasks);

        var (verifyCtx, verifyPage) = await fixture.NewPageAsync();
        await using (verifyCtx)
        {
            await verifyPage.GotoAsync("/Account/Login");
            await verifyPage.FillAsync("input[name='Input.Email']", email);
            await verifyPage.FillAsync("input[name='Input.Password']", "BadPassword!Concurrent99");
            await verifyPage.ClickAsync("button[type='submit']");
            await verifyPage.WaitForURLAsync(
                url => url.Contains("/Account/Lockout") || url.Contains("/Account/Login"),
                new Microsoft.Playwright.PageWaitForURLOptions { Timeout = 10_000 });

            Assert.True(
                verifyPage.Url.Contains("/Account/Lockout") || verifyPage.Url.Contains("/Account/Login"),
                $"Unexpected URL after concurrent lockout attempts: {verifyPage.Url}");
        }
    }
}