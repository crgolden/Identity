namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Login with Two-Factor Authentication page.</summary>
[AllowAnonymous]
#pragma warning disable S101
public class LoginWith2faModel : PageModel
#pragma warning restore S101
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ILogger<LoginWith2faModel> _logger;

    public LoginWith2faModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        UserManager<IdentityUser<Guid>> userManager,
        ILogger<LoginWith2faModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    /// <summary>Handles the GET request to display the two-factor authentication page.</summary>
    /// <param name="rememberMe">Whether the user chose to be remembered on this browser.</param>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        return Page();
    }

    /// <summary>Handles the POST request to verify the two-factor authentication code.</summary>
    /// <param name="rememberMe">Whether the user chose to be remembered on this browser.</param>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input?.TwoFactorCode))
        {
            return Page();
        }

        returnUrl ??= Url.Content("~/");

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            throw new InvalidOperationException("Unable to load two-factor authentication user.");
        }

        var authenticatorCode = Input.TwoFactorCode.Replace(" ", Empty).Replace("-", Empty);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

        var userId = await _userManager.GetUserIdAsync(user);

        if (result.Succeeded)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("User with ID '{UserId}' logged in with 2fa.", userId);
            }

            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("User with ID '{UserId}' account locked out.", userId);
            }

            return RedirectToPage("./Lockout");
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Invalid authenticator code entered for user with ID '{UserId}'.", userId);
        }

        ModelState.AddModelError(Empty, "Invalid authenticator code.");
        return Page();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string? TwoFactorCode { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }
}
