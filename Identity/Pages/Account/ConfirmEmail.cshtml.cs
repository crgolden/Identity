namespace Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class ConfirmEmail : PageModel
{
    internal const string EmailConfirmedMessage =
        "Thank you for confirming your email.";

    internal const string EmailConfirmationFailedMessage =
        "Error confirming your email.";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public ConfirmEmail(UserManager<IdentityUser<Guid>> userManager)
    {
        ThrowIfNull(userManager);
        _userManager = userManager;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (IsNullOrWhiteSpace(userId) || IsNullOrWhiteSpace(code))
        {
            return RedirectToPage(PageRoutes.Home);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(userId));
        }

        var bytes = Base64UrlDecode(code);
        code = UTF8.GetString(bytes);
        var result = await _userManager.ConfirmEmailAsync(user, code);
        StatusMessage = result.Succeeded ? EmailConfirmedMessage : EmailConfirmationFailedMessage;
        return Page();
    }
}
