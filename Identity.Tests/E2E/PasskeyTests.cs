namespace Identity.Tests.E2E;

using Infrastructure;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class PasskeyTests(PlaywrightFixture fixture)
{
    /// <summary>
    /// Verifies that <c>PasskeySubmitTagHelper</c> runs and renders a real submit button on the Passkeys
    /// management page. Without <c>@addTagHelper *, Identity.Api</c> in <c>_ViewImports.cshtml</c> the
    /// tag helper is silently ignored and the element is a non-interactive generic element, not a button.
    /// </summary>
    [Fact]
    public async Task PasskeyManagePage_RendersSubmitButton_NotGenericElement()
    {
        var (email, password) = await CreateAndLoginAsync();

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

            // The tag helper must replace <passkey-submit> with a <button type="submit">.
            // If _ViewImports.cshtml is missing @addTagHelper *, Identity.Api this assertion fails
            // because the element renders as a generic non-interactive custom element.
            var button = page.Locator("button[name='__passkeySubmit']");
            await button.WaitForAsync();
            Assert.True(await button.IsVisibleAsync(), "Add passkey button was not rendered — PasskeySubmitTagHelper may not be registered.");
            var text = await button.TextContentAsync();
            Assert.Contains("passkey", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Verifies that <c>PasskeySubmitTagHelper</c> runs and renders a real submit button on the Login page.
    /// The Login page also uses <c>&lt;passkey-submit operation="Request"&gt;</c> so both pages are affected
    /// if the tag helper registration in <c>_ViewImports.cshtml</c> is missing.
    /// </summary>
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
            Assert.True(await button.IsVisibleAsync(), "Login passkey button was not rendered — PasskeySubmitTagHelper may not be registered.");
            var text = await button.TextContentAsync();
            Assert.Contains("passkey", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<(string Email, string Password)> CreateAndLoginAsync()
    {
        var email = $"e2e-{Guid.NewGuid()}@test.invalid";
        const string password = "Test@123456!";

        var (ctx, page) = await fixture.NewPageAsync();
        await using (ctx)
        {
            await page.GotoAsync("/Account/Register");
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.FillAsync("input[name='Input.ConfirmPassword']", password);
            await page.ClickAsync("#registerSubmit");
            await page.WaitForURLAsync("**/Account/RegisterConfirmation**");

            var confirmEmail = await fixture.Email.WaitForEmailAsync(email);
            var confirmLink = EmailCaptureService.ExtractLink(confirmEmail.HtmlBody, "http");
            await page.GotoAsync(confirmLink);
            await page.WaitForURLAsync("**/Account/ConfirmEmail**");
        }

        return (email, password);
    }
}
