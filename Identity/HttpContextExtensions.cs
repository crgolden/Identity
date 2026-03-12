namespace Microsoft.AspNetCore.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Diagnostics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Mvc;
    using Mvc.Abstractions;
    using Mvc.Infrastructure;
    using Routing;
    using static StringComparer;
    using static Task;
    using static Microsoft.Net.Http.Headers.HeaderNames;
    using static WebUtilities.ReasonPhrases;

    /// <summary>A class with methods that extend <see cref="HttpContext"/>.</summary>
    public static class HttpContextExtensions
    {
        private static readonly HashSet<string> CorsHeaderNames = new HashSet<string>(OrdinalIgnoreCase)
        {
            AccessControlAllowCredentials,
            AccessControlAllowHeaders,
            AccessControlAllowMethods,
            AccessControlAllowOrigin,
            AccessControlExposeHeaders,
            AccessControlMaxAge
        };

        /// <summary>Handles the exception.</summary>
        /// <param name="context">The context.</param>
        /// <returns>A task.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
        public static Task HandleException(this HttpContext context)
        {
            if (context == default)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Response.HasStarted)
            {
                return CompletedTask;
            }

            var details = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Type = $"https://httpstatuses.com/{context.Response.StatusCode}",
                Title = GetReasonPhrase(context.Response.StatusCode)
            };

            var routeData = context.GetRouteData() ?? new RouteData();
            ClearResponse(context);
            var actionContext = new ActionContext(context, routeData, new ActionDescriptor());
            var result = new ObjectResult(details)
            {
                StatusCode = details.Status,
                DeclaredType = typeof(ProblemDetails)
            };

            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");
            var error = context.Features.Get<IExceptionHandlerFeature>().Error;
            var logger = context.RequestServices.GetService<ILogger<HttpContext>>();
            if (error != default)
            {
                logger?.LogError(error, string.Empty);
            }

            var executor = context.RequestServices.GetRequiredService<IActionResultExecutor<ObjectResult>>();
            return executor.ExecuteAsync(actionContext, result);
        }

        private static void ClearResponse(HttpContext context)
        {
            var headers = new HeaderDictionary();
            headers.Append(CacheControl, "no-cache, no-store, must-revalidate");
            headers.Append(Pragma, "no-cache");
            headers.Append(Expires, "0");
            foreach (var header in context.Response.Headers.Where(x => CorsHeaderNames.Contains(x.Key)))
            {
                headers.Add(header);
            }

            var statusCode = context.Response.StatusCode;
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            foreach (var header in headers)
            {
                context.Response.Headers.Add(header);
            }
        }
    }
}