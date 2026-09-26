namespace Identity.Tests.E2E.Oidc;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class OidcDiscoveryTests(PlaywrightFixture fixture)
{
    private static readonly string[] RequiredDiscoveryFields =
    [
        OidcDiscoveryConstants.Issuer,
        OidcDiscoveryConstants.AuthorizationEndpoint,
        OidcDiscoveryConstants.TokenEndpoint,
        OidcDiscoveryConstants.JwksUri,
        OidcDiscoveryConstants.ResponseTypesSupported,
        OidcDiscoveryConstants.SubjectTypesSupported,
        OidcDiscoveryConstants.IdTokenSigningAlgValuesSupported
    ];

    private static readonly HttpClient KestrelClient = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

    [Fact]
    public async Task Discovery_ReturnsOkWithRequiredFields()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync(
            OidcDiscoveryConstants.DiscoveryPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        foreach (var field in RequiredDiscoveryFields)
        {
            Assert.True(json.TryGetProperty(field, out _), $"Missing discovery field: {field}");
        }
    }

    [Fact]
    public async Task Discovery_IssuerIsHttps()
    {
        var response = await KestrelClient.GetAsync(
            new Uri(new Uri(fixture.BaseAddress), OidcDiscoveryConstants.DiscoveryPath),
            TestContext.Current.CancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        var issuer = json.GetProperty(OidcDiscoveryConstants.Issuer).GetString();
        Assert.NotNull(issuer);
        Assert.StartsWith(OidcDiscoveryConstants.HttpsScheme, issuer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Jwks_ContainsAtLeastOneSigningKey()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var discoveryResponse = await client.GetAsync(
            OidcDiscoveryConstants.DiscoveryPath,
            TestContext.Current.CancellationToken);
        var discovery = await discoveryResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        var jwksUri = discovery.GetProperty(OidcDiscoveryConstants.JwksUri).GetString();
        Assert.NotNull(jwksUri);

        var jwksPath = new Uri(jwksUri).PathAndQuery;

        var jwksResponse = await client.GetAsync(jwksPath, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, jwksResponse.StatusCode);

        var jwks = await jwksResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        var keys = jwks.GetProperty(OidcDiscoveryConstants.JwksKeys).EnumerateArray().ToList();
        Assert.NotEmpty(keys);
    }

    [Fact]
    public async Task Token_MissingGrantType_ReturnsBadRequest()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.PostAsync(
            OidcDiscoveryConstants.TokenPath,
            new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationEndpoint_MissingRequiredParams_RedirectsToErrorPage()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync(OidcDiscoveryConstants.AuthorizePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.RedirectMethod, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        Assert.True(location.IsAbsoluteUri);
        Assert.Equal(PageRoutes.Error, location.AbsolutePath);
    }
}
