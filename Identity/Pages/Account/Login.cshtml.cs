namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Channels;
using Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

/// <summary>Page model for the Login page.</summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ChannelWriter<Func<IServiceProvider, CancellationToken, Task>> _backgroundWriter;
    private readonly ILogger<LoginModel> _logger;
    private readonly ICAPTCHAService _captchaService;
    private readonly IOptions<ReCAPTCHAOptions> _recaptchaOptions;

    public LoginModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        ChannelWriter<Func<IServiceProvider, CancellationToken, Task>> backgroundWriter,
        ILogger<LoginModel> logger,
        ICAPTCHAService captchaService,
        IOptions<ReCAPTCHAOptions> recaptchaOptions)
    {
        _signInManager = signInManager;
        _backgroundWriter = backgroundWriter;
        _logger = logger;
        _captchaService = captchaService;
        _recaptchaOptions = recaptchaOptions;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public string? ReturnUrl { get; set; }

    public string? RecaptchaSiteKey { get; private set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>Handles the GET request to display the login page.</summary>
    /// <param name="returnUrl">The URL to return to after login.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!IsNullOrWhiteSpace(ErrorMessage))
        {
            ModelState.AddModelError(Empty, ErrorMessage);
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        ReturnUrl = returnUrl;
        ReturnUrl ??= Url.Content("~/");
        RecaptchaSiteKey = _recaptchaOptions.Value.SiteKey;
    }

    /// <summary>Handles the POST request to authenticate the user.</summary>
    /// <param name="returnUrl">The URL to return to after login.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        Microsoft.AspNetCore.Identity.SignInResult result;
        if (!IsNullOrWhiteSpace(Input.Passkey?.CredentialJson))
        {
            ModelState.Clear();
            using var passkeyActivity = Telemetry.ActivitySource.StartActivity("identity.login.passkey");
            result = await _signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
            passkeyActivity?.SetTag("succeeded", result.Succeeded);
        }
        else
        {
            if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email) || IsNullOrWhiteSpace(Input.Password))
            {
                return Page();
            }

            if (!string.Equals(Input.Email, _recaptchaOptions.Value.AdminEmail, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Input.Email, _recaptchaOptions.Value.TestEmail, StringComparison.OrdinalIgnoreCase))
            {
                var score = await _captchaService.VerifyAsync(Input.RecaptchaToken, HttpContext.RequestAborted);
                if (score < _recaptchaOptions.Value.ScoreThreshold)
                {
                    ModelState.AddModelError(Empty, "Request could not be verified.");
                    return Page();
                }
            }

            using var passwordActivity = Telemetry.ActivitySource.StartActivity("identity.login.password");
            result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
            passwordActivity?.SetTag("locked_out", result.IsLockedOut);
            passwordActivity?.SetTag("requires_2fa", result.RequiresTwoFactor);
        }

        if (result.Succeeded)
        {
            if (!IsNullOrWhiteSpace(Input.Email))
            {
                TryAddAvatarClaim(Input.Email);
            }

            _logger.LogTrace("User logged in.");
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            _logger.LogTrace("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(Empty, "Invalid login attempt.");
        return Page();
    }

    private void TryAddAvatarClaim(string email)
    {
        _backgroundWriter.TryWrite(async (sp, ct) =>
        {
            var avatarService = sp.GetRequiredService<IAvatarService>();
            var userManager = sp.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var avatarUrl = await avatarService.GetAvatarUrlAsync(email, ct);
            if (avatarUrl is null)
            {
                return;
            }

            var existingUser = await userManager.FindByNameAsync(email);
            if (existingUser is null)
            {
                return;
            }

            var claims = await userManager.GetClaimsAsync(existingUser);
            if (!claims.Any(x => string.Equals("picture", x.Type, StringComparison.Ordinal)))
            {
                await userManager.AddClaimAsync(existingUser, new Claim("picture", avatarUrl.ToString()));
            }
        });
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public PasskeyInputModel? Passkey { get; set; }

        public string? RecaptchaToken { get; set; }
    }
}
