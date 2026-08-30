namespace Identity.Pages;

using System.Diagnostics;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
#pragma warning disable S4502 // Error pages must accept GET/POST without a valid CSRF token
[IgnoreAntiforgeryToken]
#pragma warning restore S4502
public class ErrorModel : PageModel
{
    internal const string OidcErrorActivityName = "identity.error.oidc";

    internal const string OidcErrorIdTagName = "oidc.error_id";

    internal const string OidcErrorTagName = "oidc.error";

    internal const string OidcErrorDescriptionTagName = "oidc.error_description";

    private readonly IIdentityServerInteractionService _interactionService;

    public ErrorModel(IIdentityServerInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !IsNullOrWhiteSpace(RequestId);

    public async Task OnGetAsync(string? errorId = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (!IsNullOrWhiteSpace(errorId))
        {
            var errorMessage = await _interactionService.GetErrorContextAsync(errorId, HttpContext.RequestAborted);
            using var activity = Telemetry.StartActivity(OidcErrorActivityName);
            activity?.SetTag(OidcErrorIdTagName, errorId);
            activity?.SetTag(OidcErrorTagName, errorMessage?.Error);
            activity?.SetTag(OidcErrorDescriptionTagName, errorMessage?.ErrorDescription);
        }
    }
}