namespace Identity.Extensions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Provides extension methods for <see cref="PageModel"/> instances used in IdentityServer flows.</summary>
public static class PageModelExtensions
{
    /// <summary>
    /// Redirects a native (non-browser) client to the loading page, which performs a
    /// client-side redirect to <paramref name="redirectUri"/> so the user sees a brief
    /// "please wait" message rather than a blank page during protocol response processing.
    /// </summary>
    /// <param name="page">The current page model.</param>
    /// <param name="redirectUri">The URI to redirect to after the loading page renders.</param>
    /// <returns>A redirect result targeting <c>/Redirect</c>.</returns>
    public static IActionResult LoadingPage(this PageModel page, string redirectUri) =>
        page.RedirectToPage("/Redirect", new { redirectUri });
}
