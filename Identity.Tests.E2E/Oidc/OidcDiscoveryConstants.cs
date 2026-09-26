namespace Identity.Tests.E2E.Oidc;

internal static class OidcDiscoveryConstants
{
    internal const string DiscoveryPath = "/.well-known/openid-configuration";

    internal const string JwksPath = "/.well-known/openid-configuration/jwks";

    internal const string TokenPath = "/connect/token";

    internal const string AuthorizePath = "/connect/authorize";

    internal const string Issuer = "issuer";

    internal const string AuthorizationEndpoint = "authorization_endpoint";

    internal const string TokenEndpoint = "token_endpoint";

    internal const string JwksUri = "jwks_uri";

    internal const string ResponseTypesSupported = "response_types_supported";

    internal const string SubjectTypesSupported = "subject_types_supported";

    internal const string IdTokenSigningAlgValuesSupported = "id_token_signing_alg_values_supported";

    internal const string JwksKeys = "keys";

    internal const string HttpsScheme = "https://";
}
