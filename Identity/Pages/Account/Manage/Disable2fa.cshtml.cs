namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class Disable2fa : PageModel
{
    internal const string TwoFactorNotEnabledMessage = "Cannot disable 2FA for user as it's not currently enabled.";

    internal const string DisableFailedMessage = "Unexpected error occurred disabling 2FA.";

    internal const string TwoFactorDisabledMessage =
        "2fa has been disabled. You can reenable 2fa when you setup an authenticator app";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public Disable2fa(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new InvalidOperationException($"Cannot disable 2FA for user as it's not currently enabled.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disable2faResult.Succeeded)
        {
            throw new InvalidOperationException($"Unexpected error occurred disabling 2FA.");
        }

        StatusMessage = "2fa has been disabled. You can reenable 2fa when you setup an authenticator app";
        return RedirectToPage(PageRoutes.SiblingTwoFactorAuthentication);
    }
}
