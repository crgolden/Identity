namespace Identity.Tests.Oidc;

using System.Net;
using System.Text.Json;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class OidcDiscoveryTests(PlaywrightFixture fixture)
{
    private static readonly string[] RequiredDiscoveryFields =
    [
        "issuer",
        "authorization_endpoint",
        "token_endpoint",
        "jwks_uri",
        "response_types_supported",
        "subject_types_supported",
        "id_token_signing_alg_values_supported"
    ];

    [Fact]
    public async Task Discovery_ReturnsOkWithRequiredFields()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync(
            "/.well-known/openid-configuration",
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
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync(
            "/.well-known/openid-configuration",
            TestContext.Current.CancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        var issuer = json.GetProperty("issuer").GetString();
        Assert.NotNull(issuer);
        Assert.StartsWith("https://", issuer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Jwks_ContainsAtLeastOneSigningKey()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Get discovery doc to find the JWKS URI.
        var discoveryResponse = await client.GetAsync(
            "/.well-known/openid-configuration",
            TestContext.Current.CancellationToken);
        var discovery = await discoveryResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        var jwksUri = discovery.GetProperty("jwks_uri").GetString();
        Assert.NotNull(jwksUri);

        // Extract relative path from the absolute URI.
        var jwksPath = new Uri(jwksUri).PathAndQuery;

        var jwksResponse = await client.GetAsync(jwksPath, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, jwksResponse.StatusCode);

        var jwks = await jwksResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        var keys = jwks.GetProperty("keys").EnumerateArray().ToList();
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
            "/connect/token",
            new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()),
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationEndpoint_MissingRequiredParams_ReturnsBadRequest()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/connect/authorize", TestContext.Current.CancellationToken);

        // IdentityServer returns 302 to error page or 400 for missing params
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
