namespace Identity.Filters;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SecurityHeadersAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is PageResult)
        {
            var headers = context.HttpContext.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers.ContentSecurityPolicy =
                    "default-src 'self'; " +
                    "script-src 'self' https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com; " +
                    "style-src 'self' https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https:; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self';";
            }
        }

        base.OnResultExecuting(context);
    }
}