namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Identity.CAPTCHA;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class Login : PageModel
{
    internal const string LoginWith2faPageName = "./LoginWith2fa";

    internal const string PasskeyActivityName = "identity.login.passkey";

    internal const string PasswordActivityName = "identity.login.password";

    internal const string LockedOutTagName = "locked_out";

    internal const string RequiresTwoFactorTagName = "requires_2fa";

    internal const string CaptchaFailedMessage = "Request could not be verified.";

    internal const string InvalidLoginMessage = "Invalid login attempt.";

    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ICAPTCHAService _captchaService;
    private readonly Telemetry _telemetry;

    public Login(
        SignInManager<IdentityUser<Guid>> signInManager,
        ICAPTCHAService captchaService,
        Telemetry telemetry)
    {
        ThrowIfNull(signInManager);
        ThrowIfNull(captchaService);
        _signInManager = signInManager;
        _captchaService = captchaService;
        _telemetry = telemetry;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public string? ReturnUrl { get; set; }

    public string? RecaptchaSiteKey { get; private set; }

    public Uri? RecaptchaScriptEndpoint { get; private set; }

    public bool PasskeyAutofillAllowed { get; private set; } = true;

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? ErrorCode { get; set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!IsNullOrWhiteSpace(ErrorMessage))
        {
            ModelState.AddModelError(Empty, ErrorMessage);
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        ReturnUrl = returnUrl;
        ReturnUrl ??= Url.Content(PageRoutes.ContentRoot);
        RecaptchaSiteKey = _captchaService.SiteKey;
        RecaptchaScriptEndpoint = _captchaService.ScriptEndpoint;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content(PageRoutes.ContentRoot);
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        Microsoft.AspNetCore.Identity.SignInResult result;
        if (!IsNullOrWhiteSpace(Input.Passkey?.CredentialJson))
        {
            ModelState.Clear();
            using var passkeyActivity = _telemetry.StartActivity(PasskeyActivityName);
            result = await _signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
            passkeyActivity?.SetTag(Telemetry.Metrics.SucceededTagName, result.Succeeded);
            _telemetry.PasskeySignIn(result.Succeeded, HttpContext.Request.Headers.UserAgent);
            PasskeyAutofillAllowed = result.Succeeded;
        }
        else
        {
            if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email) || IsNullOrWhiteSpace(Input.Password))
            {
                return Page();
            }

            var verdict = await _captchaService.VerifyAsync(Input.RecaptchaToken, HttpContext.RequestAborted);
            if (!verdict.Passed)
            {
                ModelState.AddModelError(Empty, CaptchaFailedMessage);
                return Page();
            }

            using var passwordActivity = _telemetry.StartActivity(PasswordActivityName);
            result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
            passwordActivity?.SetTag(LockedOutTagName, result.IsLockedOut);
            passwordActivity?.SetTag(RequiresTwoFactorTagName, result.RequiresTwoFactor);
        }

        if (result.Succeeded)
        {
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect(PageRoutes.ContentRoot);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage(LoginWith2faPageName, new { ReturnUrl = returnUrl, Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage(PageRoutes.SiblingLockout);
        }

        ModelState.AddModelError(Empty, InvalidLoginMessage);
        return Page();
    }

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
