namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class GrantsTests(PlaywrightFixture fixture)
{
    /// <summary>Verifies that the grants page loads for an authenticated user with no existing grants.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Grants_AuthenticatedUser_PageLoads()
    {
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.FillAsync("input[name='Input.Email']", fixture.SharedEmail);
            await page.FillAsync("input[name='Input.Password']", fixture.SharedPassword);
            await page.ClickAsync("#login-submit");
            await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));

            await page.GotoAsync("/Account/Manage/Grants");
            await page.WaitForURLAsync("**/Account/Manage/Grants**");

            // Page should load successfully without error
            Assert.DoesNotContain("/Account/Login", page.Url);
            Assert.DoesNotContain("/Error", page.Url);
        }
    }
}
