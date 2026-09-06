namespace Identity.Tests.E2E.Security;

using System.Text.Json;
using Infrastructure;
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

        var (context, page) = await fixture.NewPageAsync("Security");
        await using (context)
        {
            await page.GotoAsync("/Account/Login");

            var known = await RequestOptionsAsync(page, email);
            var unknown = await RequestOptionsAsync(page, unknownEmail);

            Assert.Equal(known.Status, unknown.Status);
            Assert.Equal(
                known.Shape,
                unknown.Shape);
        }
    }

    private static async Task<(int Status, string Shape)> RequestOptionsAsync(IPage page, string username)
    {
        var tokenName = await page.GetAttributeAsync("passkey-submit", "request-token-name");
        var tokenValue = await page.GetAttributeAsync("passkey-submit", "request-token-value");
        Assert.False(string.IsNullOrWhiteSpace(tokenName));
        Assert.False(string.IsNullOrWhiteSpace(tokenValue));

        var raw = await page.EvaluateAsync<string>(
            """
            async ([username, tokenName, tokenValue]) => {
                const response = await fetch(
                    `/Account/PasskeyRequestOptions?username=${encodeURIComponent(username)}`,
                    { method: 'POST', credentials: 'include', headers: { [tokenName]: tokenValue } });
                const body = response.ok ? await response.text() : '';
                return JSON.stringify({ status: response.status, body });
            }
            """,
            new[] { username, tokenName, tokenValue });

        using var envelope = JsonDocument.Parse(raw);
        var status = envelope.RootElement.GetProperty("status").GetInt32();
        var body = envelope.RootElement.GetProperty("body").GetString();
        return (status, DescribeShape(body));
    }

    private static string DescribeShape(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "(empty)";
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var names = root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal);
        var allowCredentials = root.TryGetProperty("allowCredentials", out var allow)
            ? allow.GetArrayLength().ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "absent";
        return $"properties=[{string.Join(",", names)}] allowCredentials={allowCredentials}";
    }
}