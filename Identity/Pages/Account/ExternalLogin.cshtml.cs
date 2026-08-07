namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private const string LoginPageName = "./Login";
    private const string From = "noreply@crgolden.com";
    private const string EmailVerifiedClaimType = "email_verified";

    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly IUserStore<IdentityUser<Guid>> _userStore;
    private readonly IUserEmailStore<IdentityUser<Guid>> _emailStore;
    private readonly ServiceBusClient _serviceBusClient;

    public ExternalLoginModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        UserManager<IdentityUser<Guid>> userManager,
        IUserStore<IdentityUser<Guid>> userStore,
        IAzureClientFactory<ServiceBusClient> serviceBusClientFactory)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = (IUserEmailStore<IdentityUser<Guid>>)_userStore;
        _serviceBusClient = serviceBusClientFactory.CreateClient("crgolden");
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public string? ProviderDisplayName { get; set; }

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet() => RedirectToPage(LoginPageName);

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

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
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/");
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        var externalEmail = info.Principal.HasClaim(c => string.Equals(c.Type, ClaimTypes.Email))
            ? info.Principal.FindFirstValue(ClaimTypes.Email)
            : null;
        if (IsNullOrWhiteSpace(externalEmail))
        {
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            return Page();
        }

        var existingUser = await _userManager.FindByEmailAsync(externalEmail);
        if (existingUser is not null)
        {
            ErrorMessage = $"An account already exists for {externalEmail}. " +
                $"Log in with that account, then link your {info.ProviderDisplayName} account from the External Logins page.";
            return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
        }

        var user = new IdentityUser<Guid>();
        await _userStore.SetUserNameAsync(user, externalEmail, HttpContext.RequestAborted);
        await _emailStore.SetEmailAsync(user, externalEmail, HttpContext.RequestAborted);
        if (bool.TryParse(info.Principal.FindFirstValue(EmailVerifiedClaimType), out var emailVerified) && emailVerified)
        {
            await _emailStore.SetEmailConfirmedAsync(user, true, HttpContext.RequestAborted);
        }

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            ErrorMessage = Join(" ", createResult.Errors.Select(e => e.Description));
            return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
        }

        var redirect = await CompleteExternalRegistrationAsync(user, info, externalEmail, returnUrl);
        if (redirect is not null)
        {
            return redirect;
        }

        ErrorMessage = $"Unable to link your {info.ProviderDisplayName} account.";
        return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
        }

        if (ModelState.IsValid && !IsNullOrWhiteSpace(Input.Email))
        {
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser is not null)
            {
                ErrorMessage = $"An account already exists for {Input.Email}. " +
                    $"Log in with that account, then link your {info.ProviderDisplayName} account from the External Logins page.";
                return RedirectToPage(LoginPageName, new { ReturnUrl = returnUrl });
            }

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

        await _userManager.AddMissingClaimsAsync(user, info.Principal);

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
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
                var link = HtmlEncoder.Default.Encode(callbackUrl);
                var htmlMessage = $"Please confirm your account by <a href='{link}'>clicking here</a>.";
                var sbMessage = new ServiceBusMessage(htmlMessage)
                {
                    ReplyTo = From,
                    Subject = "Confirm your email",
                    To = email
                };
                var serviceBusSender = _serviceBusClient.CreateSender("email");
                await serviceBusSender.SendMessageAsync(sbMessage, HttpContext.RequestAborted);
            }

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage("./RegisterConfirmation", new { email });
            }
        }

        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/");
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}