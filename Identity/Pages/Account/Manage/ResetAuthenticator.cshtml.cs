namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Reset Authenticator page.</summary>
public class ResetAuthenticatorModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ILogger<ResetAuthenticatorModel> _logger;

    public ResetAuthenticatorModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        ILogger<ResetAuthenticatorModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>Handles the GET request to display the reset authenticator confirmation page.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        return Page();
    }

    /// <summary>Handles the POST request to reset the user's authenticator app key.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        var userId = await _userManager.GetUserIdAsync(user);
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("User with ID '{UserId}' has reset their authentication app key.", userId);
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.";
        return RedirectToPage("./EnableAuthenticator");
    }
}
