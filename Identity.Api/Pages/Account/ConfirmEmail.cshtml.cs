namespace Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Confirm Email page.</summary>
[AllowAnonymous]
public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public ConfirmEmailModel(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>Handles the GET request to confirm the user's email address.</summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="code">The base64url-encoded email confirmation token.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (IsNullOrWhiteSpace(userId) || IsNullOrWhiteSpace(code))
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userId}'.");
        }

        var bytes = Base64UrlDecode(code);
        code = UTF8.GetString(bytes);
        var result = await _userManager.ConfirmEmailAsync(user, code);
        StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";
        return Page();
    }
}
