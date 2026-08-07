namespace Identity.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class RedirectModel : PageModel
{
    public string? RedirectUri { get; set; }

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