namespace Identity.Pages;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the client-initiated backchannel authentication (CIBA) consent screen.</summary>
[AllowAnonymous]
[SecurityHeaders]
public class CibaModel : PageModel
{
    private readonly IBackchannelAuthenticationInteractionService _backchannelInteraction;
    private readonly ILogger<CibaModel> _logger;

    public CibaModel(
        IBackchannelAuthenticationInteractionService backchannelInteraction,
        ILogger<CibaModel> logger)
    {
        _backchannelInteraction = backchannelInteraction;
        _logger = logger;
    }

    /// <summary>Gets or sets the login request loaded for this CIBA session.</summary>
    public BackchannelUserLoginRequest? LoginRequest { get; set; }

    /// <summary>Handles the GET request to load and display the CIBA authorization prompt.</summary>
    /// <param name="id">The internal backchannel authentication request ID.</param>
    /// <returns>A task resolving to the page result, or a redirect to the error page if the ID is invalid.</returns>
    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (IsNullOrWhiteSpace(id))
        {
            return RedirectToPage("/Error");
        }

        var result = await _backchannelInteraction.GetLoginRequestByInternalIdAsync(id);
        if (result == null)
        {
            _logger.LogWarning("Invalid backchannel authentication login ID: {Id}", id);
            return RedirectToPage("/Error");
        }

        LoginRequest = result;
        return Page();
    }
}
