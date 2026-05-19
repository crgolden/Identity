namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

/// <summary>Page model for the Register page.</summary>
[AllowAnonymous]
public class RegisterModel : PageModel
{
    private const string From = "noreply@crgolden.com";
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ChannelWriter<string> _pictureClaimWriter;
    private readonly ServiceBusSender _emailSender;
    private readonly ICAPTCHAService _captchaService;

    public RegisterModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        ChannelWriter<string> pictureClaimWriter,
        IAzureClientFactory<ServiceBusSender> serviceBusSenderFactory,
        ICAPTCHAService captchaService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _pictureClaimWriter = pictureClaimWriter;
        _emailSender = serviceBusSenderFactory.CreateClient("email");
        _captchaService = captchaService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public string? ReturnUrl { get; set; }

    public string? RecaptchaSiteKey { get; private set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    /// <summary>Handles the GET request to display the registration page.</summary>
    /// <param name="returnUrl">The URL to return to after registration.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        RecaptchaSiteKey = _captchaService.SiteKey;
    }

    /// <summary>Handles the POST request to create a new user account.</summary>
    /// <param name="returnUrl">The URL to return to after registration.</param>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
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

        var user = new IdentityUser<Guid>();
        await _userManager.SetUserNameAsync(user, Input.Email);
        await _userManager.SetEmailAsync(user, Input.Email);
        using var activity = Telemetry.ActivitySource.StartActivity("identity.register");
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            activity?.SetTag("succeeded", true);
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            _pictureClaimWriter.TryWrite(Input.Email);
            var input = UTF8.GetBytes(code);
            code = Base64UrlEncode(input);
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId, code, returnUrl },
                protocol: Request.Scheme);

            var emailConfirmationSent = !IsNullOrWhiteSpace(callbackUrl);
            if (emailConfirmationSent)
            {
                var htmlMessage = $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.";
                var sbMessage = new ServiceBusMessage(htmlMessage)
                {
                    ReplyTo = From,
                    Subject = "Confirm your email",
                    To = Input.Email
                };
                await _emailSender.SendMessageAsync(sbMessage, HttpContext.RequestAborted);
            }

            activity?.SetTag("email_confirmation_sent", emailConfirmationSent);

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        activity?.SetTag("succeeded", false);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(Empty, error.Description);
        }

        return Page();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
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
