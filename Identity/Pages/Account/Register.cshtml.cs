namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Azure.Messaging.ServiceBus;
using Identity.CAPTCHA;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

[AllowAnonymous]
public class Register : PageModel
{
    internal const string RegisterConfirmationPageName = "RegisterConfirmation";

    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ICAPTCHAService _captchaService;
    private readonly AccountEmailSettings _accountEmailSettings;
    private readonly Telemetry _telemetry;

    public Register(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
        ICAPTCHAService captchaService,
        AccountEmailSettings accountEmailSettings,
        Telemetry telemetry)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _serviceBusClient = serviceBusClientFactory.CreateClient(ServiceBusNames.ClientName);
        _captchaService = captchaService;
        _accountEmailSettings = accountEmailSettings;
        _telemetry = telemetry;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public string? ReturnUrl { get; set; }

    public string? RecaptchaSiteKey { get; private set; }

    public Uri? RecaptchaScriptEndpoint { get; private set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        RecaptchaSiteKey = _captchaService.SiteKey;
        RecaptchaScriptEndpoint = _captchaService.ScriptEndpoint;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content(PageRoutes.ContentRoot);
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email) || IsNullOrWhiteSpace(Input.Password))
        {
            return Page();
        }

        var verdict = await _captchaService.VerifyAsync(Input.RecaptchaToken, HttpContext.RequestAborted);
        if (!verdict.Passed)
        {
            ModelState.AddModelError(Empty, "Request could not be verified.");
            return Page();
        }

        var user = new IdentityUser<Guid>();
        await _userManager.SetUserNameAsync(user, Input.Email);
        await _userManager.SetEmailAsync(user, Input.Email);
        using var activity = _telemetry.StartActivity("identity.register");
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            activity?.SetTag("succeeded", true);
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var input = UTF8.GetBytes(code);
            code = Base64UrlEncode(input);
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId, code, returnUrl },
                protocol: Request.Scheme);

            var emailConfirmationSent = false;
            if (!IsNullOrWhiteSpace(callbackUrl))
            {
                emailConfirmationSent = true;
                var sbMessage = new ServiceBusMessage(_accountEmailSettings.ConfirmAccountHtml(callbackUrl))
                {
                    ReplyTo = _accountEmailSettings.Sender,
                    Subject = UserMessages.ConfirmEmailSubject,
                    To = Input.Email
                };
                var emailSender = _serviceBusClient.CreateSender(ServiceBusNames.EmailQueueName);
                await emailSender.SendMessageAsync(sbMessage, HttpContext.RequestAborted);
            }

            activity?.SetTag("email_confirmation_sent", emailConfirmationSent);

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage(RegisterConfirmationPageName, new { email = Input.Email, returnUrl });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect(PageRoutes.ContentRoot);
        }

        activity?.SetTag("succeeded", false);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(Empty, error.Description);
        }

        return Page();
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        public string? RecaptchaToken { get; set; }
    }
}
