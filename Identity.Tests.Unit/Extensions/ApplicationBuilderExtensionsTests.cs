namespace Identity.Tests.Unit.Extensions;

using Identity.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ApplicationBuilderExtensionsTests
{
    private const string ExpectedContentSecurityPolicy =
        "default-src 'self'; script-src 'self' https://www.google.com https://www.gstatic.com; " +
        "style-src 'self'; img-src 'self' data: https:; connect-src 'self' https://www.google.com; " +
        "frame-src https://www.google.com; object-src 'none'; frame-ancestors 'none'; base-uri 'self';";

    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsXContentTypeOptionsNosniff()
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal("nosniff", (string?)context.Response.Headers.XContentTypeOptions);
    }

    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsXFrameOptionsDenyMatchingFrameAncestorsNone()
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal("DENY", (string?)context.Response.Headers.XFrameOptions);
    }

    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsReferrerPolicyNoReferrer()
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal("no-referrer", (string?)context.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsDefaultContentSecurityPolicy()
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal(ExpectedContentSecurityPolicy, (string?)context.Response.Headers.ContentSecurityPolicy);
    }

    [Theory]
    [InlineData("script-src", "https://www.google.com")]
    [InlineData("script-src", "https://www.gstatic.com")]
    [InlineData("connect-src", "https://www.google.com")]
    [InlineData("frame-src", "https://www.google.com")]
    public async Task UseSecurityHeaders_CspAllowsRecaptchaHost(string directive, string host)
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var csp = (string?)context.Response.Headers.ContentSecurityPolicy;
        Assert.NotNull(csp);
        var clause = csp.Split(';').Single(x => x.Trim().StartsWith(directive, StringComparison.Ordinal));
        Assert.Contains(host, clause, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://cdn.jsdelivr.net")]
    [InlineData("https://code.jquery.com")]
    [InlineData("https://cdnjs.cloudflare.com")]
    public async Task UseSecurityHeaders_CspExcludesSelfHostedLibraryCdn(string host)
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var csp = (string?)context.Response.Headers.ContentSecurityPolicy;
        Assert.NotNull(csp);
        Assert.DoesNotContain(host, csp, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UseSecurityHeaders_CspAllowsExternalClientLogoImages()
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var csp = (string?)context.Response.Headers.ContentSecurityPolicy;
        Assert.NotNull(csp);
        var clause = csp.Split(';').Single(x => x.Trim().StartsWith("img-src", StringComparison.Ordinal));
        Assert.Contains("https:", clause, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("text/css")]
    [InlineData("application/javascript")]
    [InlineData("application/json")]
    [InlineData(null)]
    public async Task UseSecurityHeaders_NonHtmlResponse_DoesNotSetAnyHeaders(string? contentType)
    {
        // Arrange
        var (context, responseFeature) = MakeContext(contentType);

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var headers = context.Response.Headers;
        Assert.False(headers.ContainsKey("X-Content-Type-Options"));
        Assert.False(headers.ContainsKey("X-Frame-Options"));
        Assert.False(headers.ContainsKey("Referrer-Policy"));
        Assert.False(headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task UseSecurityHeaders_ExistingCspNotOverwritten()
    {
        // Arrange
        var (context, responseFeature) = MakeContext("text/html; charset=utf-8");
        context.Response.Headers.ContentSecurityPolicy = "script-src 'none'";

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal("script-src 'none'", (string?)context.Response.Headers.ContentSecurityPolicy);
        Assert.Equal("nosniff", (string?)context.Response.Headers.XContentTypeOptions);
        Assert.Equal("DENY", (string?)context.Response.Headers.XFrameOptions);
        Assert.Equal("no-referrer", (string?)context.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public void UseSecurityHeaders_NullApplicationBuilder_Throws()
    {
        // Arrange
        IApplicationBuilder applicationBuilder = null!;

        // Act
        var exception = Record.Exception(() => applicationBuilder.UseSecurityHeaders());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    private static (DefaultHttpContext Context, CapturingResponseFeature ResponseFeature) MakeContext(string? contentType)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        var responseFeature = new CapturingResponseFeature();
        features.Set<IHttpResponseFeature>(responseFeature);

        var context = new DefaultHttpContext(features);
        if (contentType is not null)
        {
            context.Response.ContentType = contentType;
        }

        return (context, responseFeature);
    }

    private static async Task RunAsync(HttpContext context, CapturingResponseFeature responseFeature)
    {
        var applicationBuilder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        applicationBuilder.UseSecurityHeaders();
        await applicationBuilder.Build()(context);
        await responseFeature.FireOnStartingAsync();
    }

    private sealed class CapturingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _callbacks = [];

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted { get; private set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public string? ReasonPhrase { get; set; }

        public int StatusCode { get; set; } = 200;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state) => _callbacks.Add((callback, state));

        public async Task FireOnStartingAsync()
        {
            HasStarted = true;
            foreach (var (callback, state) in _callbacks)
            {
                await callback(state);
            }
        }
    }
}
