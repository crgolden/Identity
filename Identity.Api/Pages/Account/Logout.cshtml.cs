namespace Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Logout page.</summary>
[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(SignInManager<IdentityUser<Guid>> signInManager, ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>Handles the GET request to sign out the current user.</summary>
    /// <param name="returnUrl">The URL to return to after signing out.</param>
    /// <returns>A task that resolves to a redirect after signing out.</returns>
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null) =>
        await SignOutAndRedirectAsync(returnUrl);

    /// <summary>Handles the POST request to sign out the current user.</summary>
    /// <param name="returnUrl">The URL to return to after signing out.</param>
    /// <returns>A task that resolves to a redirect after signing out.</returns>
    public async Task<IActionResult> OnPost(string? returnUrl = null) =>
        await SignOutAndRedirectAsync(returnUrl);

    private async Task<IActionResult> SignOutAndRedirectAsync(string? returnUrl)
    {
        await _signInManager.SignOutAsync();
        _logger.LogTrace("User logged out.");
        return LocalRedirect(IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
