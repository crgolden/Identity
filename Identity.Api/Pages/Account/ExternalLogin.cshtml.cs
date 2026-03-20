namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the External Login page.</summary>
[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private const string LoginPageName = "./Login";

    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly IUserStore<IdentityUser<Guid>> _userStore;
    private readonly IUserEmailStore<IdentityUser<Guid>> _emailStore;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        UserManager<IdentityUser<Guid>> userManager,
        IUserStore<IdentityUser<Guid>> userStore,
        ILogger<ExternalLoginModel> logger,
        IEmailSender emailSender)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = (IUserEmailStore<IdentityUser<Guid>>)_userStore;
        _logger = logger;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public string? ProviderDisplayName { get; set; }

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>Handles the GET request by redirecting to the login page.</summary>
    /// <returns>A redirect to the login page.</returns>
    public IActionResult OnGet() => RedirectToPage(LoginPageName);

    /// <summary>Handles the POST request to initiate an external authentication challenge.</summary>
    /// <param name="provider">The external authentication provider name.</param>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <returns>A challenge result for the specified provider.</returns>
    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    /// <summary>Handles the external login callback from the provider.</summary>
    /// <param name="returnUrl">The URL to return to after login.</param>
    /// <param name="remoteError">An error message from the external provider, if any.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        if (!IsNullOrWhiteSpace(remoteError))
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name, info.LoginProvider);
            }

            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;
        if (info.Principal.HasClaim(c => string.Equals(c.Type, ClaimTypes.Email)))
        {
            Input = new InputModel
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email)
            };
        }

        return Page();
    }

    /// <summary>Handles the POST request to confirm external login registration.</summary>
    /// <param name="returnUrl">The URL to return to after confirmation.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
        }

        if (ModelState.IsValid && !IsNullOrWhiteSpace(Input?.Email))
        {
            var user = new IdentityUser<Guid>();
            await _userStore.SetUserNameAsync(user, Input.Email, HttpContext.RequestAborted);
            await _emailStore.SetEmailAsync(user, Input.Email, HttpContext.RequestAborted);
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                var redirect = await CompleteExternalRegistrationAsync(user, info, Input.Email, returnUrl);
                if (redirect is not null)
                {
                    return redirect;
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(Empty, error.Description);
            }
        }

        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        return Page();
    }

    private async Task<IActionResult?> CompleteExternalRegistrationAsync(
        IdentityUser<Guid> user,
        ExternalLoginInfo info,
        string email,
        string returnUrl)
    {
        var result = await _userManager.AddLoginAsync(user, info);
        if (!result.Succeeded)
        {
            return null;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("User created an account using {Name} provider.", info.LoginProvider);
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var bytes = UTF8.GetBytes(code);
        code = Base64UrlEncode(bytes);
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId, code },
            protocol: Request.Scheme);

        if (!IsNullOrWhiteSpace(callbackUrl))
        {
            const string subject = "Confirm your email";
            var link = HtmlEncoder.Default.Encode(callbackUrl);
            var htmlMessage = $"Please confirm your account by <a href='{link}'>clicking here</a>.";
            await _emailSender.SendEmailAsync(email, subject, htmlMessage);
        }

        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
            return RedirectToPage("./RegisterConfirmation", new { email });
        }

        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return LocalRedirect(returnUrl);
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
