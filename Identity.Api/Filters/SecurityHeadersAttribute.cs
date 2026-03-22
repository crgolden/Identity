namespace Identity.Filters;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Action filter that adds HTTP security headers to every IdentityServer page response.
/// Apply this attribute to Razor Page models that participate in IdentityServer flows
/// (Consent, Device, Grants, etc.) to prevent clickjacking, MIME-sniffing, and
/// cross-origin framing attacks.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SecurityHeadersAttribute : ActionFilterAttribute
{
    /// <inheritdoc/>
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is PageResult)
        {
            var headers = context.HttpContext.Response.Headers;

            // Prevent MIME-type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent the page from being embedded in an iframe
            headers["X-Frame-Options"] = "SAMEORIGIN";

            // Limit referrer information
            headers["Referrer-Policy"] = "no-referrer";

            // Strict CSP: only same-origin resources, no plugins, no external frames
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self';";
            }
        }

        base.OnResultExecuting(context);
    }
}
