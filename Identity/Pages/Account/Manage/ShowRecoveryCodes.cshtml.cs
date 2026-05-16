namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Show Recovery Codes page.</summary>
public class ShowRecoveryCodesModel : PageModel
{
    [TempData]
    public string[] RecoveryCodes { get; set; } = Array.Empty<string>();

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>Handles the GET request to display the generated recovery codes.</summary>
    /// <returns>A redirect or page result.</returns>
    public IActionResult OnGet()
    {
        if (RecoveryCodes.Length == 0)
        {
            return RedirectToPage("./TwoFactorAuthentication");
        }

        return Page();
    }
}