namespace Identity.Extensions;

public static class ApplicationBuilderExtensions
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' https://www.google.com https://www.gstatic.com; " +
        "style-src 'self'; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://www.google.com; " +
        "frame-src https://www.google.com; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self';";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder applicationBuilder)
    {
        ThrowIfNull(applicationBuilder);

        return applicationBuilder.Use((context, next) =>
        {
            context.Response.OnStarting(static state => ApplyHeaders((HttpContext)state), context);
            return next(context);
        });
    }

    private static Task ApplyHeaders(HttpContext context)
    {
        var response = context.Response;
        if (response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Task.CompletedTask;
        }

        var headers = response.Headers;

        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }

        return Task.CompletedTask;
    }
}
