namespace Identity.Tests.E2E.Infrastructure;

using Duende.IdentityModel;
using Duende.IdentityServer;

internal static class OidcStandardConstants
{
    internal const string OpenIdScope = IdentityServerConstants.StandardScopes.OpenId;

    internal const string ProfileScope = IdentityServerConstants.StandardScopes.Profile;

    internal const string SubjectClaim = JwtClaimTypes.Subject;

    internal const string NameClaim = JwtClaimTypes.Name;

    internal const string EmailClaim = JwtClaimTypes.Email;

    internal const string OidcProtocol = IdentityServerConstants.ProtocolTypes.OpenIdConnect;

    internal const string AuthorizationCodeGrant = OidcConstants.GrantTypes.AuthorizationCode;

    internal const string LoopbackRedirectUri = "https://localhost:9999/callback";

    internal const string LoopbackHost = "localhost:9999";

    internal const string ClientIdParameter = OidcConstants.AuthorizeRequest.ClientId;

    internal const string RedirectUriParameter = OidcConstants.AuthorizeRequest.RedirectUri;

    internal const string ResponseTypeParameter = OidcConstants.AuthorizeRequest.ResponseType;

    internal const string ScopeParameter = OidcConstants.AuthorizeRequest.Scope;

    internal const string StateParameter = OidcConstants.AuthorizeRequest.State;

    internal const string CodeResponseType = OidcConstants.ResponseTypes.Code;

    internal const string ClaimValueTrue = "true";

    internal const string ClaimValueFalse = "false";
}
