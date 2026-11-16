namespace Identity.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class Redirect : PageModel
{
    public Uri? RedirectUri { get; set; }

    public IActionResult OnGet(Uri? redirectUri)
    {
        if (!Url.IsLocalUrl(redirectUri?.OriginalString))
        {
            return RedirectToPage(PageRoutes.Error);
        }

        RedirectUri = redirectUri;
        return Page();
    }
}
