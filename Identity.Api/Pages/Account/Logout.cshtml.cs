namespace Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null) =>
        await SignOutAndRedirectAsync(returnUrl);

    public async Task<IActionResult> OnPost(string? returnUrl = null) =>
        await SignOutAndRedirectAsync(returnUrl);

    private async Task<IActionResult> SignOutAndRedirectAsync(string? returnUrl)
    {
        await _signInManager.SignOutAsync();
        _logger.LogTrace("User logged out.");
        return LocalRedirect(IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
