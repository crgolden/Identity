namespace Identity.Pages.Redirect;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Page model for the native client redirect landing page.
/// Native (non-browser) clients cannot handle protocol responses directly in the browser,
/// so IdentityServer redirects them here first. This page then performs a client-side
/// redirect to the actual redirect URI so the user sees a brief loading message.
/// </summary>
[AllowAnonymous]
public class IndexModel : PageModel
{
    /// <summary>Gets or sets the redirect URI to forward the user to after rendering.</summary>
    public string? RedirectUri { get; set; }

    /// <summary>Handles the GET request to validate and set the redirect URI.</summary>
    /// <param name="redirectUri">The URI to redirect to, which must be a local URL.</param>
    /// <returns>A redirect to the error page if the URI is not local; otherwise the page.</returns>
    public IActionResult OnGet(string? redirectUri)
    {
        if (!Url.IsLocalUrl(redirectUri))
        {
            return RedirectToPage("/Error");
        }

        RedirectUri = redirectUri;
        return Page();
    }
}
