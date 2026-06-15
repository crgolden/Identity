namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class PasskeyTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task PasskeyManagePage_RendersSubmitButton_NotGenericElement()
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

            await page.GotoAsync("/Account/Manage/Passkeys");
            await page.WaitForLoadStateAsync();

            var button = page.Locator("button[name='__passkeySubmit']");
            await button.WaitForAsync();
            Assert.True(await button.IsVisibleAsync(), "Add passkey button was not rendered; PasskeySubmitTagHelper may not be registered.");
            var text = await button.TextContentAsync();
            Assert.Contains("passkey", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task LoginPage_RendersPasskeySubmitButton_NotGenericElement()
    {
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync("/Account/Login");
            await page.WaitForLoadStateAsync();

            // The tag helper must replace <passkey-submit operation="Request"> with a <button type="submit">.
            var button = page.Locator("button[name='__passkeySubmit']");
            await button.WaitForAsync();
            Assert.True(await button.IsVisibleAsync(), "Login passkey button was not rendered; PasskeySubmitTagHelper may not be registered.");
            var text = await button.TextContentAsync();
            Assert.Contains("passkey", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
