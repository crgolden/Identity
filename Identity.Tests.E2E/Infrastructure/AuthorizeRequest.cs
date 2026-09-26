namespace Identity.Tests.E2E.Infrastructure;

using Identity.Tests.E2E.Oidc;

internal static class AuthorizeRequest
{
    internal static string Url(string clientId, string redirectUri, string state) =>
        $"{OidcDiscoveryConstants.AuthorizePath}" +
        $"?{OidcStandardConstants.ClientIdParameter}={Uri.EscapeDataString(clientId)}" +
        $"&{OidcStandardConstants.RedirectUriParameter}={Uri.EscapeDataString(redirectUri)}" +
        $"&{OidcStandardConstants.ResponseTypeParameter}={OidcStandardConstants.CodeResponseType}" +
        $"&{OidcStandardConstants.ScopeParameter}={OidcStandardConstants.OpenIdScope}" +
        $"&{OidcStandardConstants.StateParameter}={Uri.EscapeDataString(state)}";
}
