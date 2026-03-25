namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Login page.</summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly IAvatarService _avatarService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        UserManager<IdentityUser<Guid>> userManager,
        IAvatarService avatarService,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _avatarService = avatarService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public string? ReturnUrl { get; set; }

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
            result = await _signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
        }
        else
        {
            if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email) || IsNullOrWhiteSpace(Input.Password))
            {
                return Page();
            }

            result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        }

        if (result.Succeeded)
        {
            if (!IsNullOrWhiteSpace(Input.Email))
            {
                await TryAddAvatarClaimAsync(Input.Email, HttpContext.RequestAborted);
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

    private async Task TryAddAvatarClaimAsync(string email, CancellationToken cancellationToken)
    {
        var avatarUrl = await _avatarService.GetAvatarUrlAsync(email, cancellationToken);
        if (avatarUrl is null)
        {
            return;
        }

        var user = await _userManager.FindByNameAsync(email);
        if (user is null)
        {
            return;
        }

        var userClaims = await _userManager.GetClaimsAsync(user);
        if (!userClaims.Any(x => string.Equals("picture", x.Type, StringComparison.Ordinal)))
        {
            await _userManager.AddClaimAsync(user, new Claim("picture", avatarUrl.ToString()));
        }
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
    }
}
