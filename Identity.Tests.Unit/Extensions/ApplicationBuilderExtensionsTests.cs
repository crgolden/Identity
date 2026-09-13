namespace Identity.Tests.Unit.Extensions;

using System.Net.Mime;
using System.Text;
using Identity.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using SecurityHeaders = Identity.Extensions.ApplicationBuilderExtensions;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsXContentTypeOptionsNosniff()
    {
        // Arrange
        var (context, responseFeature) = MakeContext(HtmlContentType());

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal(
            SecurityHeaders.ContentTypeOptionsNoSniff,
            (string?)context.Response.Headers.XContentTypeOptions);
    }

    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsXFrameOptionsDenyMatchingFrameAncestorsNone()
    {
        // Arrange
        var (context, responseFeature) = MakeContext(HtmlContentType());

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal(
            SecurityHeaders.FrameOptionsDeny,
            (string?)context.Response.Headers.XFrameOptions);
    }

    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsReferrerPolicyNoReferrer()
    {
        // Arrange
        var (context, responseFeature) = MakeContext(HtmlContentType());

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal(
            SecurityHeaders.ReferrerPolicyNoReferrer,
            (string?)context.Response.Headers[SecurityHeaders.ReferrerPolicyHeaderName]);
    }

    [Fact]
    public async Task UseSecurityHeaders_HtmlResponse_SetsDefaultContentSecurityPolicy()
    {
        // Arrange
        var (context, responseFeature) = MakeContext(HtmlContentType());

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal(
            SecurityHeaders.ContentSecurityPolicy,
            (string?)context.Response.Headers.ContentSecurityPolicy);
    }

    [Theory]
    [InlineData(SecurityHeaders.ScriptSrcDirective, SecurityHeaders.GoogleRecaptchaHost)]
    [InlineData(SecurityHeaders.ScriptSrcDirective, SecurityHeaders.GoogleStaticHost)]
    [InlineData(SecurityHeaders.ConnectSrcDirective, SecurityHeaders.GoogleRecaptchaHost)]
    [InlineData(SecurityHeaders.FrameSrcDirective, SecurityHeaders.GoogleRecaptchaHost)]
    public async Task UseSecurityHeaders_CspAllowsRecaptchaHost(string directive, string host)
    {
        // Arrange
        var (context, responseFeature) = MakeContext(HtmlContentType());

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var csp = (string?)context.Response.Headers.ContentSecurityPolicy;
        Assert.NotNull(csp);
        Assert.Contains(host, ClauseFor(csp, directive), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UseSecurityHeaders_CspNamesOnlyTheAllowedExternalHosts()
    {
        // Arrange
        var (context, responseFeature) = MakeContext(HtmlContentType());

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var csp = (string?)context.Response.Headers.ContentSecurityPolicy;
        Assert.NotNull(csp);
        Assert.Equal(
            [SecurityHeaders.GoogleRecaptchaHost, SecurityHeaders.GoogleStaticHost],
            ExternalHostsIn(csp));
    }

    [Fact]
    public async Task UseSecurityHeaders_CspAllowsExternalClientLogoImages()
    {
        // Arrange
        var (context, responseFeature) = MakeContext(HtmlContentType());

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var csp = (string?)context.Response.Headers.ContentSecurityPolicy;
        Assert.NotNull(csp);
        Assert.Contains(
            SecurityHeaders.AnyHttpsSource,
            ClauseFor(csp, SecurityHeaders.ImgSrcDirective),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(MediaTypeNames.Text.Css)]
    [InlineData(MediaTypeNames.Text.JavaScript)]
    [InlineData(MediaTypeNames.Application.Json)]
    [InlineData(null)]
    public async Task UseSecurityHeaders_NonHtmlResponse_DoesNotSetAnyHeaders(string? contentType)
    {
        // Arrange
        var (context, responseFeature) = MakeContext(contentType);

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        var headers = context.Response.Headers;
        Assert.False(headers.ContainsKey(HeaderNames.XContentTypeOptions));
        Assert.False(headers.ContainsKey(HeaderNames.XFrameOptions));
        Assert.False(headers.ContainsKey(SecurityHeaders.ReferrerPolicyHeaderName));
        Assert.False(headers.ContainsKey(HeaderNames.ContentSecurityPolicy));
    }

    [Fact]
    public async Task UseSecurityHeaders_ExistingCspNotOverwritten()
    {
        // Arrange
        var existingPolicy = SecurityHeaders.ScriptSrcDirective + ' ' + TestValues.NewPolicyDirectiveSource();
        var (context, responseFeature) = MakeContext(HtmlContentType());
        context.Response.Headers.ContentSecurityPolicy = existingPolicy;

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal(existingPolicy, (string?)context.Response.Headers.ContentSecurityPolicy);
        Assert.Equal(
            SecurityHeaders.ContentTypeOptionsNoSniff,
            (string?)context.Response.Headers.XContentTypeOptions);
        Assert.Equal(
            SecurityHeaders.FrameOptionsDeny,
            (string?)context.Response.Headers.XFrameOptions);
        Assert.Equal(
            SecurityHeaders.ReferrerPolicyNoReferrer,
            (string?)context.Response.Headers[SecurityHeaders.ReferrerPolicyHeaderName]);
    }

    [Fact]
    public void UseSecurityHeaders_NullApplicationBuilder_Throws()
    {
        // Arrange
        IApplicationBuilder? applicationBuilder = null;

        // Act
        var exception = Record.Exception(() => applicationBuilder.UseSecurityHeaders());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    private static string HtmlContentType() =>
        new MediaTypeHeaderValue(MediaTypeNames.Text.Html) { Charset = Encoding.UTF8.WebName }.ToString();

    private static string ClauseFor(string contentSecurityPolicy, string directive) =>
        contentSecurityPolicy.Split(';').Single(clause => clause.Trim().StartsWith(directive, StringComparison.Ordinal));

    private static string[] ExternalHostsIn(string contentSecurityPolicy) =>
        contentSecurityPolicy
            .Split([';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.StartsWith(Uri.UriSchemeHttps + Uri.SchemeDelimiter, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

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

        public int StatusCode { get; set; } = StatusCodes.Status200OK;

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
