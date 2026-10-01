namespace Identity.Tests.Integration.Security;

using System.Text.Json;
using Identity.Extensions;
using Identity.Pages.Account.Manage;
using Identity.Tests.Integration.Infrastructure;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class PasskeyRequestOptionsTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Does_not_reveal_whether_an_account_exists()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();
        var unknownEmail = $"absent-{Guid.NewGuid()}@test.invalid";
        using var client = AccountSession.NewClient(fixture);
        var page = await client.GetStringAsync(PageRoutes.Login, TestContext.Current.CancellationToken);
        var tokenName = AccountSession.ReadValue(
            page, HtmlFormConstants.AttributeValuePattern, PasskeySubmitTagHelper.RequestTokenNameAttributeName);
        var tokenValue = AccountSession.ReadValue(
            page, HtmlFormConstants.AttributeValuePattern, PasskeySubmitTagHelper.RequestTokenValueAttributeName);

        var known = await RequestOptionsAsync(client, email, tokenName, tokenValue);
        var unknown = await RequestOptionsAsync(client, unknownEmail, tokenName, tokenValue);

        Assert.Equal(known.Status, unknown.Status);
        Assert.Equal(known.Properties, unknown.Properties);
        Assert.Equal(known.AllowCredentials, unknown.AllowCredentials);
    }

    private static async Task<(int Status, string[] Properties, int? AllowCredentials)> RequestOptionsAsync(
        HttpClient client,
        string username,
        string tokenName,
        string tokenValue)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{PasskeyEndpoints.RequestOptionsPath}?{PasskeyEndpoints.UserNameQueryKey}={Uri.EscapeDataString(username)}");
        request.Headers.Add(tokenName, tokenValue);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
            : null;
        var (properties, allowCredentials) = DescribeShape(body);
        return ((int)response.StatusCode, properties, allowCredentials);
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
