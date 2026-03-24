namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class ServerSideSessionsTests(PlaywrightFixture fixture)
{
    /// <summary>Verifies that after login the server-side sessions page loads without error.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ServerSideSessions_AfterLogin_PageLoads()
    {
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", fixture.SharedEmail);
            await page.FillAsync("input[name='Input.Password']", fixture.SharedPassword);
            await page.ClickAsync("#login-submit");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            await page.GotoAsync("/Account/Manage/ServerSideSessions");
            await page.WaitForURLAsync("**/Account/Manage/ServerSideSessions**");

            // Page should load successfully (not redirect to error or login)
            Assert.DoesNotContain("/Account/Login", page.Url);
            Assert.DoesNotContain("/Error", page.Url);
        }
    }
}
