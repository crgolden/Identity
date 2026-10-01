namespace Identity.Tests.Integration.Infrastructure;

using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Identity.Pages.Account;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

internal static class AccountSession
{
    public static HttpClient NewClient(IdentityHostFixture fixture) =>
        fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new UriBuilder(fixture.Factory.ClientOptions.BaseAddress) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri,
        });

    public static async Task<HttpClient> SignedInClientAsync(IdentityHostFixture fixture, string email, string password)
    {
        var client = NewClient(fixture);
        using var response = await SubmitLoginAsync(fixture, client, email, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotEqual(PageRoutes.Login, LocationPath(client, response));
        return client;
    }

    public static async Task<HttpResponseMessage> SubmitLoginAsync(
        IdentityHostFixture fixture,
        HttpClient client,
        string email,
        string password)
    {
        var formFieldName = fixture.Factory.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.FormFieldName;
        var page = await client.GetStringAsync(PageRoutes.Login, TestContext.Current.CancellationToken);
        var fields = new Dictionary<string, string>
        {
            [formFieldName] = ReadValue(page, HtmlFormConstants.HiddenInputValuePattern, formFieldName),
            [$"{nameof(Login.Input)}.{nameof(Login.InputModel.Email)}"] = email,
            [$"{nameof(Login.Input)}.{nameof(Login.InputModel.Password)}"] = password,
        };
        using var form = new FormUrlEncodedContent(fields);
        return await client.PostAsync(PageRoutes.Login, form, TestContext.Current.CancellationToken);
    }

    public static string ReadValue(string html, string patternFormat, string name)
    {
        var pattern = string.Format(CultureInfo.InvariantCulture, patternFormat, Regex.Escape(name));
        var match = Regex.Match(html, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        Assert.True(match.Success, $"The page carries no value for {name}.");
        return WebUtility.HtmlDecode(match.Groups[HtmlFormConstants.ValueGroupName].Value);
    }

    public static string? RedirectPath(HttpClient client, HttpResponseMessage response) =>
        response.Headers.Location is { } location && client.BaseAddress is { } baseAddress
            ? new Uri(baseAddress, location).AbsolutePath
            : null;

    public static string LocationPath(HttpClient client, HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        Assert.NotNull(location);
        var baseAddress = client.BaseAddress;
        Assert.NotNull(baseAddress);
        return new Uri(baseAddress, location).AbsolutePath;
    }
}
