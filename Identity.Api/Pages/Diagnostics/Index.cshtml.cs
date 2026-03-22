namespace Identity.Pages.Diagnostics;

using System.Security.Claims;
using Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Page model for the diagnostics page, which displays the current user's claims and
/// authentication tokens. Intended for development and troubleshooting only.
/// </summary>
[Authorize]
[SecurityHeaders]
public class IndexModel : PageModel
{
    /// <summary>Gets or sets the diagnostics view model built from the current authentication context.</summary>
    public DiagnosticsViewModel View { get; set; } = new DiagnosticsViewModel();

    /// <summary>Handles the GET request to populate the diagnostics view model.</summary>
    /// <returns>A task resolving to the page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var localAddresses = new[] { "127.0.0.1", "::1", HttpContext.Connection.LocalIpAddress?.ToString() };
        if (!localAddresses.Contains(HttpContext.Connection.RemoteIpAddress?.ToString()))
        {
            return NotFound();
        }

        var authenticateResult = await HttpContext.AuthenticateAsync();
        View = new DiagnosticsViewModel(authenticateResult);
        return Page();
    }

    /// <summary>View model containing the current user's claims and authentication tokens.</summary>
    public class DiagnosticsViewModel
    {
        public DiagnosticsViewModel()
        {
        }

        public DiagnosticsViewModel(AuthenticateResult result)
        {
            AuthenticateResult = result;
            if (result.Properties != null)
            {
                Tokens = result.Properties.GetTokens()
                    .Select(t => new AuthenticationTokenViewModel { Name = t.Name, Value = t.Value })
                    .ToList();
            }
        }

        /// <summary>Gets the authentication result.</summary>
        public AuthenticateResult? AuthenticateResult { get; }

        /// <summary>Gets the claims from the authenticated user.</summary>
        public IEnumerable<Claim> Claims => AuthenticateResult?.Principal?.Claims ?? [];

        /// <summary>Gets the authentication tokens stored in the authentication properties.</summary>
        public IEnumerable<AuthenticationTokenViewModel> Tokens { get; } = [];
    }

    /// <summary>View model for a single authentication token.</summary>
    public class AuthenticationTokenViewModel
    {
        /// <summary>Gets or sets the token name.</summary>
        public string Name { get; set; } = Empty;

        /// <summary>Gets or sets the token value.</summary>
        public string? Value { get; set; }
    }
}
