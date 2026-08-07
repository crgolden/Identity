namespace Identity.Pages.Account.Manage;

using System.Security.Claims;
using Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize]
[SecurityHeaders]
public class DiagnosticsModel : PageModel
{
    public DiagnosticsViewModel View { get; set; } = new DiagnosticsViewModel();

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

        public AuthenticateResult? AuthenticateResult { get; }

        public IEnumerable<Claim> Claims => AuthenticateResult?.Principal?.Claims ?? [];

        public IEnumerable<AuthenticationTokenViewModel> Tokens { get; } = [];
    }

    public class AuthenticationTokenViewModel
    {
        public string Name { get; set; } = Empty;

        public string? Value { get; set; }
    }
}