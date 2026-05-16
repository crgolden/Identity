namespace Identity.Pages.Account;

using Duende.IdentityServer.Services;
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
    private readonly IIdentityServerInteractionService _interactionService;

    public LogoutModel(
        SignInManager<IdentityUser<Guid>> signInManager,
        ILogger<LogoutModel> logger,
        IIdentityServerInteractionService interactionService)
    {
        _signInManager = signInManager;
        _logger = logger;
        _interactionService = interactionService;
    }

    /// <summary>Gets the post-logout redirect URI supplied by the client, if any.</summary>
    public string? PostLogoutRedirectUri { get; private set; }

    /// <summary>Gets the front-channel sign-out iframe URL for notifying other clients, if any.</summary>
    public string? SignOutIFrameUrl { get; private set; }

    /// <summary>Gets a value indicating whether the logout confirmation prompt should be shown.</summary>
    public bool ShowLogoutPrompt { get; private set; }

    /// <summary>Handles the GET request. Shows a confirmation prompt if the user is still authenticated.</summary>
    /// <param name="logoutId">The IdentityServer logout identifier, if initiated by a client.</param>
    /// <returns>A task that resolves to the page result.</returns>
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

    /// <summary>Handles the POST request to sign out the current user.</summary>
    /// <param name="logoutId">The IdentityServer logout identifier, if initiated by a client.</param>
    /// <returns>A task that resolves to the page result after signing out.</returns>
    public async Task<IActionResult> OnPostAsync(string? logoutId = null)
    {
        await _signInManager.SignOutAsync();
        _logger.LogTrace("User logged out.");
        await SetLogoutContextAsync(logoutId);
        return Page();
    }

    private async Task SetLogoutContextAsync(string? logoutId)
    {
        if (IsNullOrWhiteSpace(logoutId))
        {
            return;
        }

        var context = await _interactionService.GetLogoutContextAsync(logoutId);
        PostLogoutRedirectUri = context.PostLogoutRedirectUri;
        SignOutIFrameUrl = context.SignOutIFrameUrl;
    }
}
