namespace Identity.Tests.Unit.Extensions;

using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using Duende.IdentityModel;
using Identity.Extensions;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Core;
using Serilog.Events;

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
            global::Identity.Extensions.ApplicationBuilderExtensions.ContentTypeOptionsNoSniff,
            context.Response.Headers.XContentTypeOptions);
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
            global::Identity.Extensions.ApplicationBuilderExtensions.FrameOptionsDeny,
            context.Response.Headers.XFrameOptions);
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
            global::Identity.Extensions.ApplicationBuilderExtensions.ReferrerPolicyNoReferrer,
            context.Response.Headers[global::Identity.Extensions.ApplicationBuilderExtensions.ReferrerPolicyHeaderName]);
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
            global::Identity.Extensions.ApplicationBuilderExtensions.ContentSecurityPolicy,
            context.Response.Headers.ContentSecurityPolicy);
    }

    [Theory]
    [InlineData(global::Identity.Extensions.ApplicationBuilderExtensions.ScriptSrcDirective, global::Identity.Extensions.ApplicationBuilderExtensions.GoogleRecaptchaHost)]
    [InlineData(global::Identity.Extensions.ApplicationBuilderExtensions.ScriptSrcDirective, global::Identity.Extensions.ApplicationBuilderExtensions.GoogleStaticHost)]
    [InlineData(global::Identity.Extensions.ApplicationBuilderExtensions.ConnectSrcDirective, global::Identity.Extensions.ApplicationBuilderExtensions.GoogleRecaptchaHost)]
    [InlineData(global::Identity.Extensions.ApplicationBuilderExtensions.FrameSrcDirective, global::Identity.Extensions.ApplicationBuilderExtensions.GoogleRecaptchaHost)]
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
            [global::Identity.Extensions.ApplicationBuilderExtensions.GoogleRecaptchaHost, global::Identity.Extensions.ApplicationBuilderExtensions.GoogleStaticHost],
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
            global::Identity.Extensions.ApplicationBuilderExtensions.AnyHttpsSource,
            ClauseFor(csp, global::Identity.Extensions.ApplicationBuilderExtensions.ImgSrcDirective),
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
        Assert.False(headers.ContainsKey(global::Identity.Extensions.ApplicationBuilderExtensions.ReferrerPolicyHeaderName));
        Assert.False(headers.ContainsKey(HeaderNames.ContentSecurityPolicy));
    }

    [Fact]
    public async Task UseSecurityHeaders_ExistingCspNotOverwritten()
    {
        // Arrange
        var existingPolicy = global::Identity.Extensions.ApplicationBuilderExtensions.ScriptSrcDirective + ' ' + Generated.NewPolicyDirectiveSource();
        var (context, responseFeature) = MakeContext(HtmlContentType());
        context.Response.Headers.ContentSecurityPolicy = existingPolicy;

        // Act
        await RunAsync(context, responseFeature);

        // Assert
        Assert.Equal(existingPolicy, context.Response.Headers.ContentSecurityPolicy);
        Assert.Equal(
            global::Identity.Extensions.ApplicationBuilderExtensions.ContentTypeOptionsNoSniff,
            context.Response.Headers.XContentTypeOptions);
        Assert.Equal(
            global::Identity.Extensions.ApplicationBuilderExtensions.FrameOptionsDeny,
            context.Response.Headers.XFrameOptions);
        Assert.Equal(
            global::Identity.Extensions.ApplicationBuilderExtensions.ReferrerPolicyNoReferrer,
            context.Response.Headers[global::Identity.Extensions.ApplicationBuilderExtensions.ReferrerPolicyHeaderName]);
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

    [Fact]
    public void UseUserLogContext_NullApplicationBuilder_Throws()
    {
        // Arrange
        IApplicationBuilder? applicationBuilder = null;

        // Act
        var exception = Record.Exception(() => applicationBuilder.UseUserLogContext());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task UseUserLogContext_AuthenticatedUser_AddsTheSubjectAndEmailToEveryEventLoggedDownstream()
    {
        // Arrange
        var subject = Generated.NewIdentitySub().ToString();
        var email = Generated.NewEmailAddress();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(JwtClaimTypes.Subject, subject), new Claim(JwtClaimTypes.Email, email)],
                Generated.NewTokenFromFirstHalfOfAlphabet(8))),
        };

        // Act
        var logged = await LogDownstreamOfUserLogContextAsync(context);

        // Assert
        Assert.Equal(
            subject,
            Assert.IsType<ScalarValue>(logged.Properties[global::Identity.Extensions.ApplicationBuilderExtensions.UserIdLogProperty]).Value);
        Assert.Equal(
            email,
            Assert.IsType<ScalarValue>(logged.Properties[global::Identity.Extensions.ApplicationBuilderExtensions.UserEmailLogProperty]).Value);
    }

    [Fact]
    public async Task UseUserLogContext_AnonymousRequest_AddsNoUserPropertiesToEventsLoggedDownstream()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var logged = await LogDownstreamOfUserLogContextAsync(context);

        // Assert
        Assert.False(logged.Properties.ContainsKey(global::Identity.Extensions.ApplicationBuilderExtensions.UserIdLogProperty));
        Assert.False(logged.Properties.ContainsKey(global::Identity.Extensions.ApplicationBuilderExtensions.UserEmailLogProperty));
    }

    private static async Task<LogEvent> LogDownstreamOfUserLogContextAsync(HttpContext context)
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
        try
        {
            var applicationBuilder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            applicationBuilder.UseUserLogContext();
            applicationBuilder.Run(_ =>
            {
                logger.Information(Generated.NewTokenFromFirstHalfOfAlphabet(8));
                return Task.CompletedTask;
            });
            await applicationBuilder.Build()(context);
        }
        finally
        {
            await logger.DisposeAsync();
        }

        return Assert.Single(sink.Events);
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

    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
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
