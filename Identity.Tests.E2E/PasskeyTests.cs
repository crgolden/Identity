namespace Identity.Tests.E2E;

using System.Text.RegularExpressions;
using Identity.Pages.Account.Manage;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

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
            await page.GotoAsync(PageRoutes.Login);
            await page.FillAsync("input[name='Input.Email']", email);
            await page.FillAsync("input[name='Input.Password']", password);
            await page.ClickAsync("#login-submit");
            await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex(PageRoutes.Login));

            await page.GotoAsync("/Account/Manage/Passkeys");
            await page.WaitForLoadStateAsync();

            var button = page.Locator(PasskeySelectors.Register);
            await button.WaitForAsync();
            Assert.True(await button.IsVisibleAsync(), "Add passkey button was not rendered; PasskeySubmitTagHelper may not be registered.");
            Assert.Equal(
                PasskeySubmitTagHelper.SubmitButtonName,
                await button.GetAttributeAsync(PasskeySubmitTagHelper.NameAttributeName));
        }
    }

    [Fact]
    public async Task LoginPage_RendersPasskeySubmitButton_NotGenericElement()
    {
        var (context, page) = await fixture.NewPageAsync();
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);
            await page.WaitForLoadStateAsync();

            var button = page.Locator(PasskeySelectors.SignIn);
            await button.WaitForAsync();
            Assert.True(await button.IsVisibleAsync(), "Login passkey button was not rendered; PasskeySubmitTagHelper may not be registered.");
            Assert.Equal(
                PasskeySubmitTagHelper.SubmitButtonName,
                await button.GetAttributeAsync(PasskeySubmitTagHelper.NameAttributeName));
        }
    }
}
