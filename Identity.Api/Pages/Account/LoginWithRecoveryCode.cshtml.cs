namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Login with Recovery Code page.</summary>
[AllowAnonymous]
public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

    public LoginWithRecoveryCodeModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        UserManager<IdentityUser<Guid>> userManager,
        ILogger<LoginWithRecoveryCodeModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public string? ReturnUrl { get; set; }

    /// <summary>Handles the GET request to display the recovery code login page.</summary>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException("Unable to load two-factor authentication user.");
        }

        ReturnUrl = returnUrl;

        return Page();
    }

    /// <summary>Handles the POST request to authenticate using a recovery code.</summary>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input?.RecoveryCode))
        {
            return Page();
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException("Unable to load two-factor authentication user.");
        }

        var recoveryCode = Input.RecoveryCode.Replace(" ", Empty);

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        var userId = await _userManager.GetUserIdAsync(user);

        if (result.Succeeded)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("User with ID '{UserId}' logged in with a recovery code.", userId);
            }

            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        if (result.IsLockedOut)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("User account locked out.");
            }

            return RedirectToPage("./Lockout");
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Invalid recovery code entered for user with ID '{UserId}' ", userId);
        }

        ModelState.AddModelError(Empty, "Invalid recovery code entered.");
        return Page();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [BindProperty]
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string? RecoveryCode { get; set; }
    }
}
