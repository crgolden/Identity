namespace Identity.Extensions;

using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

/// <summary>Extension methods for <see cref="HttpContext"/>.</summary>
public static class HttpContextExtensions
{
    public static async Task HandleException(this HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var activity = Activity.Current;
        if (exception is not null && activity is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
            }));
        }

        Telemetry.Metrics.ExceptionOccurred(exception?.GetType().Name ?? "Unknown");

        if (context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Error");
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var problemDetailsService = context.RequestServices
                .GetRequiredService<IProblemDetailsService>();
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = { Status = StatusCodes.Status500InternalServerError },
            });
        }
    }
}
