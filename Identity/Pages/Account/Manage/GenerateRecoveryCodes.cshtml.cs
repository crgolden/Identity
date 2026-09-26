namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class GenerateRecoveryCodes : PageModel
{
    internal const int RecoveryCodeCount = 10;

    internal const string TwoFactorNotEnabledOnGetMessage =
        "Cannot generate recovery codes for user because they do not have 2FA enabled.";

    internal const string TwoFactorNotEnabledOnPostMessage =
        "Cannot generate recovery codes for user as they do not have 2FA enabled.";

    internal const string RecoveryCodesGeneratedMessage =
        "You have generated new recovery codes.";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public GenerateRecoveryCodes(UserManager<IdentityUser<Guid>> userManager)
    {
        ThrowIfNull(userManager);
        _userManager = userManager;
    }

    [TempData]
    public string[] RecoveryCodes { get; set; } = Array.Empty<string>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        return !isTwoFactorEnabled
            ? throw new InvalidOperationException(TwoFactorNotEnabledOnGetMessage)
            : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (!isTwoFactorEnabled)
        {
            throw new InvalidOperationException(TwoFactorNotEnabledOnPostMessage);
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        RecoveryCodes = recoveryCodes?.ToArray() ?? RecoveryCodes;
        StatusMessage = RecoveryCodesGeneratedMessage;
        return RedirectToPage(PageRoutes.SiblingShowRecoveryCodes);
    }
}
