namespace Identity.Extensions;

using System.Net.Mime;
using System.Security.Claims;
using Duende.IdentityModel;
using Microsoft.Net.Http.Headers;
using Serilog.Context;

public static class ApplicationBuilderExtensions
{
    internal const string UserIdLogProperty = "UserId";

    internal const string UserEmailLogProperty = "UserEmail";

    internal const string ReferrerPolicyHeaderName = "Referrer-Policy";

    internal const string ReferrerPolicyNoReferrer = "no-referrer";

    internal const string ContentTypeOptionsNoSniff = "nosniff";

    internal const string FrameOptionsDeny = "DENY";

    internal const string ScriptSrcDirective = "script-src";

    internal const string StyleSrcDirective = "style-src";

    internal const string ImgSrcDirective = "img-src";

    internal const string ConnectSrcDirective = "connect-src";

    internal const string FrameSrcDirective = "frame-src";

    internal const string GoogleRecaptchaHost = "https://www.google.com";

    internal const string GoogleStaticHost = "https://www.gstatic.com";

    internal const string AnyHttpsSource = "https:";

    internal const string ContentSecurityPolicy =
        "default-src 'self'; " +
        ScriptSrcDirective + " 'self' " + GoogleRecaptchaHost + " " + GoogleStaticHost + "; " +
        StyleSrcDirective + " 'self'; " +
        ImgSrcDirective + " 'self' data: " + AnyHttpsSource + "; " +
        ConnectSrcDirective + " 'self' " + GoogleRecaptchaHost + "; " +
        FrameSrcDirective + " " + GoogleRecaptchaHost + "; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self';";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder? applicationBuilder)
    {
        ThrowIfNull(applicationBuilder);

        return applicationBuilder.Use((context, next) =>
        {
            context.Response.OnStarting(static state => ApplyHeaders((HttpContext)state), context);
            return next(context);
        });
    }

    public static IApplicationBuilder UseUserLogContext(this IApplicationBuilder? applicationBuilder)
    {
        ThrowIfNull(applicationBuilder);

        return applicationBuilder.Use((context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return next(context);
            }

            using (LogContext.PushProperty(UserIdLogProperty, context.User.FindFirstValue(JwtClaimTypes.Subject)))
            using (LogContext.PushProperty(UserEmailLogProperty, context.User.FindFirstValue(JwtClaimTypes.Email)))
            {
                return next(context);
            }
        });
    }

    private static Task ApplyHeaders(HttpContext context)
    {
        var response = context.Response;
        if (response.ContentType?.StartsWith(MediaTypeNames.Text.Html, StringComparison.OrdinalIgnoreCase) != true)
        {
            return Task.CompletedTask;
        }

        var headers = response.Headers;

        headers.XContentTypeOptions = ContentTypeOptionsNoSniff;
        headers.XFrameOptions = FrameOptionsDeny;
        headers[ReferrerPolicyHeaderName] = ReferrerPolicyNoReferrer;

        if (!headers.ContainsKey(HeaderNames.ContentSecurityPolicy))
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }

        return Task.CompletedTask;
    }
}
