namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class DiagnosticsTests(PlaywrightFixture fixture)
{
    /// <summary>Verifies that an authenticated user can access the diagnostics page and sees claims.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Diagnostics_AuthenticatedUser_ShowsClaims()
    {
        var (email, password) = await fixture.CreateConfirmedUserAsync();

        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            await page.GotoAsync("/Account/Manage/Diagnostics");
            await page.WaitForURLAsync("**/Account/Manage/Diagnostics**");

            var content = await page.ContentAsync();
            Assert.Contains("sub", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
