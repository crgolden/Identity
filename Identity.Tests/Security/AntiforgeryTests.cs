namespace Identity.Tests.Security;

using System.Net;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class AntiforgeryTests(PlaywrightFixture fixture)
{
    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/Account/ForgotPassword")]
    [InlineData("/Account/ResendEmailConfirmation")]
    public async Task Post_WithoutAntiforgeryToken_ReturnsBadRequest(string path)
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var content = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());
        var response = await client.PostAsync(path, content, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PasskeyCreationOptions_Unauthenticated_DoesNotReturn200()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.PostAsync(
            "/Account/PasskeyCreationOptions",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PasskeyRequestOptions_Unauthenticated_DoesNotReturn200()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.PostAsync(
            "/Account/PasskeyRequestOptions",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
