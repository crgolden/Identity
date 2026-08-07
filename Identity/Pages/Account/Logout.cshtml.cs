namespace Identity.Pages.Account;

using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly IIdentityServerInteractionService _interactionService;

    public LogoutModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        IIdentityServerInteractionService interactionService)
    {
        ThrowIfNull(signInManager);
        ThrowIfNull(interactionService);
        _signInManager = signInManager;
        _interactionService = interactionService;
    }

    public string? PostLogoutRedirectUri { get; private set; }

    public string? SignOutIFrameUrl { get; private set; }

    public bool ShowLogoutPrompt { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? logoutId = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            ShowLogoutPrompt = true;
            return Page();
        }

        await SetLogoutContextAsync(logoutId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? logoutId = null)
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage(new { logoutId });
    }

    private async Task SetLogoutContextAsync(string? logoutId)
    {
        if (IsNullOrWhiteSpace(logoutId))
        {
            return;
        }

        var context = await _interactionService.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);
        PostLogoutRedirectUri = context.PostLogoutRedirectUri;
        SignOutIFrameUrl = context.SignOutIFrameUrl;
    }
}