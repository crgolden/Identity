namespace Identity.Pages;

using System.Diagnostics;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
#pragma warning disable S4502 // Error pages must accept GET/POST without a valid CSRF token
[IgnoreAntiforgeryToken]
#pragma warning restore S4502
public class Error : PageModel
{
    internal const string OidcErrorActivityName = "identity.error.oidc";

    internal const string OidcErrorIdTagName = "oidc.error_id";

    internal const string OidcErrorTagName = "oidc.error";

    internal const string OidcErrorDescriptionTagName = "oidc.error_description";

    internal const string ContinueToClientLinkId = "error-continue-to-client";

    private readonly IIdentityServerInteractionService _interactionService;
    private readonly IClientStore _clientStore;
    private readonly IReadOnlyList<ErrorExplanation> _errorExplanations;
    private readonly Telemetry _telemetry;

    public Error(
        IIdentityServerInteractionService interactionService,
        IClientStore clientStore,
        IOptions<IReadOnlyList<ErrorExplanation>> errorExplanations,
        Telemetry telemetry)
    {
        _interactionService = interactionService;
        _clientStore = clientStore;
        _errorExplanations = errorExplanations.Value;
        _telemetry = telemetry;
    }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !IsNullOrWhiteSpace(RequestId);

    public ErrorExplanation? Explanation { get; private set; }

    public string? ClientName { get; private set; }

    public Uri? ClientHomeUri { get; private set; }

    public async Task OnGetAsync(string? errorId = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (IsNullOrWhiteSpace(errorId))
        {
            return;
        }

        var errorMessage = await _interactionService.GetErrorContextAsync(errorId, HttpContext.RequestAborted);
        using (var activity = _telemetry.StartActivity(OidcErrorActivityName))
        {
            activity?.SetTag(OidcErrorIdTagName, errorId);
            activity?.SetTag(OidcErrorTagName, errorMessage?.Error);
            activity?.SetTag(OidcErrorDescriptionTagName, errorMessage?.ErrorDescription);
        }

        if (errorMessage is null)
        {
            return;
        }

        if (!IsNullOrWhiteSpace(errorMessage.ActivityId))
        {
            RequestId = errorMessage.ActivityId;
        }

        Explanation = _errorExplanations.FirstOrDefault(explanation =>
            string.Equals(explanation.Error, errorMessage.Error, StringComparison.Ordinal));
        if (!IsNullOrWhiteSpace(errorMessage.ClientId))
        {
            await SetClientHomeAsync(errorMessage.ClientId);
        }
    }

    private static Uri? HomeUriOf(Client client)
    {
        if (Uri.TryCreate(client.ClientUri, UriKind.Absolute, out var clientUri) && IsWebUri(clientUri))
        {
            return clientUri;
        }

        var redirectOrigins = client.RedirectUris
            .Select(redirectUri => Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsed) && IsWebUri(parsed)
                ? new Uri(parsed.GetLeftPart(UriPartial.Authority))
                : null)
            .OfType<Uri>()
            .Distinct(EqualityComparer<Uri>.Default)
            .ToList();
        return redirectOrigins.Count == 1 ? redirectOrigins[0] : null;
    }

    private static bool IsWebUri(Uri uri) =>
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
        || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal);

    private async Task SetClientHomeAsync(string clientId)
    {
        var client = await _clientStore.FindClientByIdAsync(clientId, HttpContext.RequestAborted);
        if (client is not { Enabled: true })
        {
            return;
        }

        ClientName = client.ClientName;
        ClientHomeUri = HomeUriOf(client);
    }
}
