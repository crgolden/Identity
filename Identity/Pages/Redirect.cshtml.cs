namespace Identity.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class Redirect : PageModel
{
    public Uri? RedirectUri { get; set; }

    public IActionResult OnGet(string? redirectUri)
    {
        if (!Url.IsLocalUrl(redirectUri))
        {
            return RedirectToPage(PageRoutes.Error);
        }

        RedirectUri = UriValue.From(redirectUri);
        return Page();
    }
}
