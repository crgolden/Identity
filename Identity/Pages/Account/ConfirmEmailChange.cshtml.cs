namespace Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class ConfirmEmailChangeModel : PageModel
{
    internal const string EmailChangeFailedMessage =
        "Error changing email.";

    internal const string UserNameChangeFailedMessage =
        "Error changing user name.";

    internal const string EmailChangeConfirmedMessage =
        "Thank you for confirming your email change.";

    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public ConfirmEmailChangeModel(UserManager<IdentityUser<Guid>> userManager, SignInManager<IdentityUser<Guid>> signInManager)
    {
        ThrowIfNull(userManager);
        ThrowIfNull(signInManager);
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? email, string? code)
    {
        if (IsNullOrWhiteSpace(userId) || IsNullOrWhiteSpace(email) || IsNullOrWhiteSpace(code))
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
        var result = await _userManager.ChangeEmailAsync(user, email, code);
        if (!result.Succeeded)
        {
            StatusMessage = EmailChangeFailedMessage;
            return Page();
        }

        var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
        if (!setUserNameResult.Succeeded)
        {
            StatusMessage = UserNameChangeFailedMessage;
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = EmailChangeConfirmedMessage;
        return Page();
    }
}