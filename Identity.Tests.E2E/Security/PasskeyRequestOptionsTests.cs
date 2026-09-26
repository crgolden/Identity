namespace Identity.Tests.E2E.Security;

using System.Text.Json;
using Identity.Extensions;
using Identity.Pages.Account.Manage;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class PasskeyRequestOptionsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Does_not_reveal_whether_an_account_exists()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();
        var unknownEmail = $"absent-{Guid.NewGuid()}@test.invalid";

        var (context, page) = await fixture.NewPageAsync(PlaywrightSuite.Security);
        await using (context)
        {
            await page.GotoAsync(PageRoutes.Login);

            var known = await RequestOptionsAsync(page, email);
            var unknown = await RequestOptionsAsync(page, unknownEmail);

            Assert.Equal(known.Status, unknown.Status);
            Assert.Equal(known.Properties, unknown.Properties);
            Assert.Equal(known.AllowCredentials, unknown.AllowCredentials);
        }
    }

    private static async Task<(int Status, string[] Properties, int? AllowCredentials)> RequestOptionsAsync(
        IPage page,
        string username)
    {
        var tokenName = await page.GetAttributeAsync(
            PasskeySubmitTagHelper.TagName, PasskeySubmitTagHelper.RequestTokenNameAttributeName);
        var tokenValue = await page.GetAttributeAsync(
            PasskeySubmitTagHelper.TagName, PasskeySubmitTagHelper.RequestTokenValueAttributeName);
        Assert.False(string.IsNullOrWhiteSpace(tokenName));
        Assert.False(string.IsNullOrWhiteSpace(tokenValue));

        var raw = await page.EvaluateAsync<string>(
            BrowserScripts.PasskeyRequestOptionsFetch,
            new[] { PasskeyEndpoints.RequestOptionsPath, username, tokenName, tokenValue });

        using var envelope = JsonDocument.Parse(raw);
        var status = envelope.RootElement.GetProperty("status").GetInt32();
        var body = envelope.RootElement.GetProperty("body").GetString();
        var (properties, allowCredentials) = DescribeShape(body);
        return (status, properties, allowCredentials);
    }

    private static (string[] Properties, int? AllowCredentials) DescribeShape(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return ([], null);
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var names = root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        return (
            names,
            root.TryGetProperty("allowCredentials", out var allow) ? allow.GetArrayLength() : null);
    }
}
