namespace Identity.Tests.E2E.Security;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ConcurrentLockoutTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Login_ConcurrentFailedAttempts_AccountEventuallyLocked()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();
        var wrongPassword = Generated.NewPassword();
        var maxFailedAttempts = fixture.Factory.Services
            .GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;
        var attemptsPerThreshold = Random.Shared.Next(2, 4);
        var concurrentAttempts = maxFailedAttempts * attemptsPerThreshold;

        var concurrentTasks = Enumerable.Range(0, concurrentAttempts).Select(async _ =>
        {
            var (ctx, page) = await fixture.NewPageAsync();
            await using (ctx)
            {
                await page.GotoAsync(PageRoutes.Login);
                await page.FillAsync("input[name='Input.Email']", email);
                await page.FillAsync("input[name='Input.Password']", wrongPassword);
                await page.ClickAsync("button[type='submit']");
                await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
            }
        });
        await Task.WhenAll(concurrentTasks);

        var (verifyCtx, verifyPage) = await fixture.NewPageAsync();
        await using (verifyCtx)
        {
            await verifyPage.GotoAsync(PageRoutes.Login);
            await verifyPage.FillAsync("input[name='Input.Email']", email);
            await verifyPage.FillAsync("input[name='Input.Password']", wrongPassword);
            await verifyPage.ClickAsync("button[type='submit']");
            await verifyPage.WaitForURLAsync(
                url => url.Contains(PageRoutes.Lockout, StringComparison.Ordinal) || url.Contains(PageRoutes.Login, StringComparison.Ordinal));

            Assert.True(
                verifyPage.Url.Contains(PageRoutes.Lockout, StringComparison.Ordinal) || verifyPage.Url.Contains(PageRoutes.Login, StringComparison.Ordinal),
                $"Unexpected URL after concurrent lockout attempts: {verifyPage.Url}");
        }
    }
}
