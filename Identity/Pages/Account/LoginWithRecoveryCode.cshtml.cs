namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class LoginWithRecoveryCodeModel : PageModel
{
    internal const string InvalidRecoveryCodeMessage =
        "Invalid recovery code entered.";

    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public LoginWithRecoveryCodeModel(SignInManager<IdentityUser<Guid>> signInManager)
    {
        ThrowIfNull(signInManager);
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException(UserMessages.UnableToLoadTwoFactorUser);
        }

        ReturnUrl = returnUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input?.RecoveryCode))
        {
            return Page();
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException(UserMessages.UnableToLoadTwoFactorUser);
        }

        var recoveryCode = Input.RecoveryCode.Replace(" ", Empty, StringComparison.Ordinal);

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect(PageRoutes.ContentRoot);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage(PageRoutes.SiblingLockout);
        }

        ModelState.AddModelError(Empty, InvalidRecoveryCodeMessage);
        return Page();
    }

    public class InputModel
    {
        [BindProperty]
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string? RecoveryCode { get; set; }
    }
}