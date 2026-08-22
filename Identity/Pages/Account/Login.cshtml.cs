namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using CAPTCHA;
using Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ICAPTCHAService _captchaService;

    public LoginModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        ICAPTCHAService captchaService)
    {
        ThrowIfNull(signInManager);
        ThrowIfNull(captchaService);
        _signInManager = signInManager;
        _captchaService = captchaService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public string? ReturnUrl { get; set; }

    public string? RecaptchaSiteKey { get; private set; }

    [TempData]
    public string? ErrorMessage { get; set; }

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
        RecaptchaSiteKey = _captchaService.SiteKey;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        Microsoft.AspNetCore.Identity.SignInResult result;
        if (!IsNullOrWhiteSpace(Input.Passkey?.CredentialJson))
        {
            ModelState.Clear();
            using var passkeyActivity = Telemetry.StartActivity("identity.login.passkey");
            result = await _signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
            passkeyActivity?.SetTag("succeeded", result.Succeeded);
        }
        else
        {
            if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email) || IsNullOrWhiteSpace(Input.Password))
            {
                return Page();
            }

            if (!_captchaService.IsExempt(Input.Email))
            {
                var score = await _captchaService.VerifyAsync(Input.RecaptchaToken, HttpContext.RequestAborted);
                if (score < _captchaService.ScoreThreshold)
                {
                    ModelState.AddModelError(Empty, "Request could not be verified.");
                    return Page();
                }
            }

            using var passwordActivity = Telemetry.StartActivity("identity.login.password");
            result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
            passwordActivity?.SetTag("locked_out", result.IsLockedOut);
            passwordActivity?.SetTag("requires_2fa", result.RequiresTwoFactor);
        }

        if (result.Succeeded)
        {
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(Empty, "Invalid login attempt.");
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