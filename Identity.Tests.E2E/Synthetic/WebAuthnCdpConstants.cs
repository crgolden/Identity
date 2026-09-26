namespace Identity.Tests.E2E.Synthetic;

internal static class WebAuthnCdpConstants
{
    internal const string DisableMethod = "WebAuthn.disable";

    internal const string EnableMethod = "WebAuthn.enable";

    internal const string AddVirtualAuthenticatorMethod = "WebAuthn.addVirtualAuthenticator";

    internal const string AddCredentialMethod = "WebAuthn.addCredential";

    internal const string OptionsKey = "options";

    internal const string ProtocolKey = "protocol";

    internal const string TransportKey = "transport";

    internal const string HasResidentKeyKey = "hasResidentKey";

    internal const string HasUserVerificationKey = "hasUserVerification";

    internal const string IsUserVerifiedKey = "isUserVerified";

    internal const string AutomaticPresenceSimulationKey = "automaticPresenceSimulation";

    internal const string AuthenticatorIdKey = "authenticatorId";

    internal const string CredentialKey = "credential";

    internal const string CredentialIdKey = "credentialId";

    internal const string IsResidentCredentialKey = "isResidentCredential";

    internal const string RpIdKey = "rpId";

    internal const string PrivateKeyKey = "privateKey";

    internal const string UserHandleKey = "userHandle";

    internal const string SignCountKey = "signCount";

    internal const string StoredCredentialIdField = "id";

    internal const string Ctap2Protocol = "ctap2";

    internal const string InternalTransport = "internal";
}
