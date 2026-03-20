namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Personal Data page.</summary>
public class PersonalDataModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public PersonalDataModel(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Handles the GET request to display the personal data page.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        return Page();
    }
}
