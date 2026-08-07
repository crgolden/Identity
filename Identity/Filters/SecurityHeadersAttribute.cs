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
            headers.XFrameOptions = "SAMEORIGIN";
            headers["Referrer-Policy"] = "no-referrer";

            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers.ContentSecurityPolicy =
                    "default-src 'self'; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self';";
            }
        }

        base.OnResultExecuting(context);
    }
}