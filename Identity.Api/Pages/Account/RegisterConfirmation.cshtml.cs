namespace Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Register Confirmation page.</summary>
[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public RegisterConfirmationModel(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    public string? Email { get; set; }

    /// <summary>Handles the GET request to display the registration confirmation page.</summary>
    /// <param name="email">The email address that was registered.</param>
    /// <param name="returnUrl">The URL to return to after confirmation.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync(string? email, string? returnUrl = null)
    {
        if (IsNullOrWhiteSpace(email))
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return NotFound($"Unable to load user with email '{email}'.");
        }

        Email = email;

        return Page();
    }
}